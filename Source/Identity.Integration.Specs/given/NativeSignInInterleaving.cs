// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading.Channels;
using Cratis.Arc.Commands;
using Cratis.Identity.Accounts.SigningIn;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Pauses real decisions before Arc processes their returned events, isolated to the owning async flow.
/// </summary>
public sealed class NativeSignInInterleaving
{
    readonly AsyncLocal<Run?> _current = new();

    /// <summary>
    /// Starts an observation, pausing only the requested number of handler invocations.
    /// </summary>
    /// <param name="pausedDecisions">The number of decisions to pause.</param>
    /// <returns>The observation and its release controls.</returns>
    public Run Begin(int pausedDecisions)
    {
        var run = new Run(pausedDecisions, () => _current.Value = null);
        _current.Value = run;
        return run;
    }

    /// <summary>
    /// Observes the real handler's decision before returning its unchanged result to Arc.
    /// </summary>
    /// <param name="decision">The caught-up, guarded decision.</param>
    /// <returns>The pause, if this async flow owns one.</returns>
    internal Task Decided(SignInWithUpstreamKey.Decision decision) => _current.Value?.Decided(decision) ?? Task.CompletedTask;

    /// <summary>
    /// Records the unmodified result of a real protocol command.
    /// </summary>
    /// <param name="command">The executed command.</param>
    /// <param name="result">Its actual pipeline result.</param>
    public void Completed(object command, CommandResult result)
    {
        if (command is SignInWithUpstreamKey signIn)
        {
            _current.Value?.Results.Enqueue(new(signIn.OperationId, result));
        }
    }

    /// <summary>
    /// Observes entry to the real retry fence.
    /// </summary>
    public void Retrying() => _current.Value?.Retrying();

    /// <summary>
    /// The actual command result for one sign-in attempt.
    /// </summary>
    /// <param name="OperationId">The verified operation.</param>
    /// <param name="Result">The real pipeline result.</param>
    public record Attempt(FactId OperationId, CommandResult Result);

    /// <summary>
    /// Owns bounded pauses and observations for one test, including its child tasks.
    /// </summary>
    /// <param name="pausedDecisions">The number of decisions to pause.</param>
    /// <param name="end">Clears the owning flow's observation.</param>
    public sealed class Run(int pausedDecisions, Action end) : IDisposable
    {
        readonly Channel<Pause> _arrivals = Channel.CreateUnbounded<Pause>();
        readonly ConcurrentBag<Pause> _pauses = [];
        int _decisions;
        int _retryWaits;

        internal ConcurrentQueue<SignInWithUpstreamKey.Decision> Decisions { get; } = new();
        public ConcurrentQueue<Attempt> Results { get; } = new();
        public int RetryWaits => Volatile.Read(ref _retryWaits);

        /// <summary>
        /// Waits for an actual decision, not a timing window.
        /// </summary>
        /// <returns>The paused decision.</returns>
        internal async Task<Pause> Next() => await _arrivals.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));

        /// <inheritdoc/>
        public void Dispose()
        {
            end();
            foreach (var pause in _pauses)
            {
                pause.Release();
            }
        }

        internal void Retrying() => Interlocked.Increment(ref _retryWaits);

        internal async Task Decided(SignInWithUpstreamKey.Decision decision)
        {
            Decisions.Enqueue(decision);
            if (Interlocked.Increment(ref _decisions) <= pausedDecisions)
            {
                var pause = new Pause(decision);
                _pauses.Add(pause);
                await _arrivals.Writer.WriteAsync(pause);
                await pause.Released.WaitAsync(TimeSpan.FromSeconds(30));
            }
        }
    }

    /// <summary>
    /// Holds a completed real decision before its events can reach the append pipeline.
    /// </summary>
    /// <param name="decision">The decision after its lookup and account rechecks.</param>
    internal sealed class Pause(SignInWithUpstreamKey.Decision decision)
    {
        readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SignInWithUpstreamKey.Decision Decision => decision;
        internal Task Released => _released.Task;

        /// <summary>
        /// Allows Arc to process and append the real handler result.
        /// </summary>
        public void Release() => _released.TrySetResult();
    }
}
