// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Identity.Configuration;
using Cratis.Identity.Protocol;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.SigningIn.given;

public class conflicting_guarded_decisions : an_account_adapter
{
    private protected List<SignInWithUpstreamKey> _attempts = [];
    protected List<object[]> _decisionEvents = [];
    protected List<(int Fence, Type Model)> _targets = [];
    protected List<(int Reservations, int Accounts, int Decisions)> _pendingReads = [];
    protected TaskCompletionSource[] _accountReached;
    protected TaskCompletionSource[] _lookupReached;
    private protected TaskCompletionSource<ObserverPosition>[] _accountPositions;
    private protected TaskCompletionSource<ObserverPosition>[] _lookupPositions;
    int _fence;

    void Establish()
    {
        _catchUp = new(_progress, Options.Create(new IdentityOptions { Accounts = new() { ReadModelCatchUpTimeout = TimeSpan.FromSeconds(10) } }), _clock);
        _accountReached = Enumerable.Range(0, 3).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        _lookupReached = Enumerable.Range(0, 3).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        _accountPositions = Enumerable.Range(0, 3).Select(_ => new TaskCompletionSource<ObserverPosition>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        _lookupPositions = Enumerable.Range(0, 3).Select(_ => new TaskCompletionSource<ObserverPosition>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        _progress.Tail(Arg.Any<Type>()).Returns(call =>
        {
            var model = call.Arg<Type>();
            _targets.Add((_fence, model));
            return Task.FromResult(Target(_fence, model));
        });
        _progress.Position(Arg.Any<Type>()).Returns(call =>
        {
            var account = call.Arg<Type>() == typeof(Account);
            (account ? _accountReached : _lookupReached)[_fence].SetResult();
            return (account ? _accountPositions : _lookupPositions)[_fence].Task;
        });
        _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>()).Returns(call => ConflictAfterDecision(call.ArgAt<SignInWithUpstreamKey>(0)));
        _commands.BeforeRetry(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            _fence = 1;
            await _catchUp.BeforeRetry(CancellationToken.None);
        });
    }

    protected static EventSequenceNumber Target(int fence, Type model) => new((ulong)((10 * (fence + 1)) + (model == typeof(Account) ? 1 : 2)));

    protected void ObservePendingReads() => _pendingReads.Add((_order.Count(step => step == "reservation"), _order.Count(step => step == "account"), _decisionEvents.Count));

    async Task<CommandResult> ConflictAfterDecision(SignInWithUpstreamKey command)
    {
        _fence = _attempts.Count * 2;
        _attempts.Add(command);
        _command = command;
        await Decide();
        if (_refusal is not null)
        {
            return new CommandResult { ValidationResults = [ValidationResult.Error(_refusal)] };
        }

        _decisionEvents.Add(_events.ToArray());
        return new CommandResult { ValidationResults = [ValidationResult.Error("Conflict") with { Reason = ValidationResultReason.ConcurrencyViolation }] };
    }
}
