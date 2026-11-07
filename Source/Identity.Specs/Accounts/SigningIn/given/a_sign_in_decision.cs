// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.given;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.SigningIn.given;

public class a_sign_in_decision : Specification
{
    protected AccountSubject _subject;
    protected LinkId _link;
    protected Account _account;
    protected UpstreamKeyMatch _reservation;
    protected VerifiedUpstreamSignIn _verified;
    protected SignInWithUpstreamKey _command;
    protected IDecisionReads _decisions;
    protected IReadModels _readModels;
    protected IObserverProgress _progress;
    protected ReadModelCatchUp _catchUp;
    protected VerifiedKeyGate _gate;
    protected UpstreamLookups _lookups;
    protected AccountClock _clock;
    protected UpstreamLookupDigest _digest;
    protected string _refusal;
    protected IEnumerable<object> _events = [];
    protected Action _releaseProjection = () => { };
    protected List<string> _order = [];

    void Establish()
    {
        _clock = new();
        _subject = AccountSubject.New();
        _link = Guid.NewGuid();
        var authentication = new VerifiedAuthentication(GitHubConnection.FixedConnectionId, "https://github.com", "42", _clock.Now, FactId.New());
        _verified = new();
        _verified.Set(authentication, new("profile@example.test", "Person", "new-login"));
        _command = new(authentication.OperationId);
        var keys = Substitute.For<IUpstreamLookupKeys>();
        keys.Current.Returns(new UpstreamLookupKey(1, Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()));
        var digester = new UpstreamKeyDigester(keys);
        _gate = new(keys, digester);
        _digest = digester.Digest(authentication.IdpIssuer, authentication.Subject);
        _reservation = new(_subject, _link, true);
        _account = new(_subject, "Person", "profile@example.test", "42", "old-login", AccountStatus.Active, [new(_link, authentication.ConnectionId, authentication.IdpIssuer, authentication.Subject, "old-login", _clock.Now)]);
        _decisions = Substitute.For<IDecisionReads>();
        _decisions.Get<AccountGuard>(Arg.Any<ReadModelKey>()).Returns(_ =>
        {
            _order.Add("guard");
            return Task.FromResult<DecisionRead<AccountGuard>>(null!);
        });
        _readModels = Substitute.For<IReadModels>();
        _readModels.GetInstances<UpstreamKeyLookup>(default).ReturnsForAnyArgs(_ =>
        {
            _order.Add("reservation");
            return Task.FromResult<IEnumerable<UpstreamKeyLookup>>([new(_subject, [new(_link, _digest, _reservation.Active)])]);
        });
        _readModels.GetInstanceById<Account>(Arg.Any<ReadModelKey>(), default).Returns(_ =>
        {
            _order.Add("account");
            return Task.FromResult(_account);
        });
        _lookups = new(_readModels);
        _progress = Substitute.For<IObserverProgress>();
        _progress.Tail(Arg.Any<Type>()).Returns(_ =>
        {
            _order.Add("target");
            return Task.FromResult(new EventSequenceNumber(1));
        });
        _progress.Position(Arg.Any<Type>()).Returns(_ =>
        {
            _releaseProjection();
            return Task.FromResult(new ObserverPosition(ObserverRunningState.Active, false, false, new(1)));
        });
        _catchUp = new(_progress, Options.Create(new IdentityOptions { Accounts = new() { ReadModelCatchUpTimeout = TimeSpan.FromMilliseconds(100) } }), _clock);
    }

    protected async Task Decide()
    {
        var provided = await _command.Provide(_verified, _gate, _lookups, _decisions, new AccountReader(_readModels), _catchUp, _clock);
        if (provided.TryGetError(out var refusal))
        {
            _refusal = refusal.Message;
            return;
        }

        var result = _command.Handle(provided.AsT0);
        if (result.TryGetError(out refusal))
        {
            _refusal = refusal.Message;
            return;
        }

        _events = result.AsT0.Item1.Select(item => item.Event).ToArray();
    }
}
