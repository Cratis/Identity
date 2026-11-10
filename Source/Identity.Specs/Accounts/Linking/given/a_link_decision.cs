// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.given;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.Linking.given;

public class a_link_decision : Specification
{
    protected AccountSubject _account;
    protected AccountClock _clock;
    protected IDecisionReads _decisions;
    protected IReadModels _readModels;
    private protected AccountLinkState _state;
    private protected LinkCeremony _ceremony;
    private protected UpstreamLookups _lookups;
    private protected UpstreamKeyDigester _digester;
    protected IOptions<IdentityOptions> _options;
    protected List<string> _order = [];
    protected List<ReadModelKey> _guardKeys = [];
    protected List<ReadModelKey> _stateKeys = [];

    void Establish()
    {
        _clock = new();
        _account = AccountSubject.New();
        _state = new(_account.Value) { Registered = true };
        var session = EventSourcedAccountPrincipal.For(_account, "display", _clock);
        var authentication = new VerifiedAuthentication(GitHubConnection.FixedConnectionId, "https://github.com", "42", _clock.Now, FactId.New()) { SessionId = session.FindFirst("sid")!.Value };
        _ceremony = new();
        _ceremony.Set(session, authentication, new(true), CancellationToken.None);
        _options = Options.Create(new IdentityOptions());
        var keys = Substitute.For<IUpstreamLookupKeys>();
        keys.Current.Returns(new UpstreamLookupKey(1, Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()));
        _digester = new(keys);
        _decisions = Substitute.For<IDecisionReads>();
        _decisions.Get<AccountGuard>(Arg.Any<ReadModelKey>()).Returns(call =>
        {
            _order.Add("guard");
            _guardKeys.Add(call.Arg<ReadModelKey>());
            return Task.FromResult<DecisionRead<AccountGuard>>(null!);
        });
        _readModels = Substitute.For<IReadModels>();
        _readModels.GetInstanceById<AccountLinkState>(Arg.Any<ReadModelKey>(), default).Returns(call =>
        {
            _order.Add("state");
            _stateKeys.Add(call.Arg<ReadModelKey>());
            return Task.FromResult(_state);
        });
        _readModels.GetInstances<UpstreamKeyLookup>(default).ReturnsForAnyArgs(_ =>
        {
            _order.Add("reservation");
            return Task.FromResult<IEnumerable<UpstreamKeyLookup>>([]);
        });
        _lookups = new(_readModels);
    }

    protected void ConfigureScenario(IServiceCollection services)
    {
        services.AddSingleton<ILinkCeremony>(_ceremony);
        services.AddSingleton(_digester);
        services.AddSingleton(_options);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddScoped<UpstreamLookups>();
        services.AddSingleton<ICommandFilter>(new ProtocolCommandFilter());
    }
}
