// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Identity.Accounts;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Integration.for_AccountErasure.given;

/// <summary>
/// Replays the account projection and waits on Chronicle's own signals (the replay job, then the observer position),
/// never on the clock. A wait that does not finish throws with the observed state and the kernel output, so a CI
/// failure names its cause instead of a bare timeout.
/// </summary>
internal static class ReplayWaits
{
    /// <summary>The bound for each wait; a hang becomes a named failure.</summary>
    public static readonly TimeSpan Bound = TimeSpan.FromSeconds(90);

    /// <summary>Gets the sequence number the account projection must have handled, taken from its own input events.</summary>
    /// <param name="store">The event store.</param>
    /// <returns>The tail of the projection's inputs.</returns>
    public static Task<EventSequenceNumber> AccountTail(IEventStore store) => new ObserverProgress(store).Tail(typeof(Account));

    /// <summary>Replays <see cref="AccountProjection"/> and waits until it is active again at <paramref name="tail"/>.</summary>
    /// <param name="store">The event store.</param>
    /// <param name="tail">The sequence number the projection must have handled.</param>
    /// <param name="kernelOutput">Reads the kernel output, for the failure message.</param>
    /// <returns>A task that completes once the replay is finished and the projection is settled.</returns>
    /// <exception cref="TimeoutException">The replay did not finish; the message carries the diagnostics.</exception>
    /// <exception cref="InvalidOperationException">The replay finished unsuccessfully; the message carries the diagnostics.</exception>
    public static async Task ReplayAccountProjection(IEventStore store, EventSequenceNumber tail, Func<Task<string>> kernelOutput)
    {
        var jobId = await store.Projections.Replay<AccountProjection>();
        if (jobId == JobId.NotSet)
        {
            throw new InvalidOperationException(await Describe(store, "The kernel did not accept the replay (no job id).", kernelOutput));
        }

        Job? job;
        try
        {
            job = await store.Jobs.WaitTillJobCompletesOrIsDeleted(jobId, Bound);
            await store.Projections.WaitTillReachesEventSequenceNumber<AccountProjection>(tail, Bound);
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException(await Describe(store, $"The replay job {jobId} did not finish within {Bound}: {ex.Message}", kernelOutput), ex);
        }

        // A removed job (null) finished; a retained one must say it succeeded.
        var position = await new ObserverProgress(store).Position(typeof(Account));
        var succeeded = job is null || job.Status == JobStatus.CompletedSuccessfully;
        var settled = position is { HasFailedPartitions: false, IsReplaying: false, RunningState: ObserverRunningState.Active };
        if (!succeeded || !settled)
        {
            throw new InvalidOperationException(await Describe(store, $"The replay job {jobId} ended with status {job?.Status.ToString() ?? "removed"}.", kernelOutput));
        }
    }

    static async Task<string> Describe(IEventStore store, string problem, Func<Task<string>> kernelOutput)
    {
        var position = await new ObserverProgress(store).Position(typeof(Account));
        return $"{problem}\nAccount projection position: {position}\n{await kernelOutput()}";
    }
}
