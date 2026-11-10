// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Accounts.SigningIn;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.given;

public class an_account_store : Specification
{
    protected static readonly UpstreamIssuer GitHubIssuer = new("https://github.com");
    protected EventStoreForTesting _events;
    protected AccountClock _clock;
    protected IdentityOptions _options;
    private protected UpstreamKeyDigester _digester;
    private protected ReadModelScenario<Account> _profiles;
    AccountStore _store;
    private protected UpstreamKeyLinking _linking;
    private protected GitHubConnection _github;
    private protected UpstreamLookups _lookups;
    private protected ReadModelScenario<AccountLinkState> _states;
    private protected ReadModelScenario<UpstreamKeyLookup> _reservations;

    readonly HashSet<AccountSubject> _subjects = [];

    void Establish()
    {
        _events = new();
        _clock = new();
        _options = new();
        var keys = Substitute.For<IUpstreamLookupKeys>();
        keys.Current.Returns(new UpstreamLookupKey(1, Enumerable.Range(0, 32).Select(number => (byte)number).ToArray()));
        _digester = new(keys);
        _github = new(Guid.Parse("d906e811-7a0b-4efc-bb47-dbd272c6b28b"), _clock);
        WireReaders();
    }

    void Destroy()
    {
        _states.Dispose();
        _reservations.Dispose();
        _profiles.Dispose();
        _events.Dispose();
    }

    private protected VerifiedAuthentication GitHub(long id)
    {
        var operation = FactId.New();
        _subjects.Add(new(operation.Value.ToString("D")));
        return _github.From(new(id, "display", "Person"), operation);
    }

    private protected VerifiedAuthentication GitHubForLink(long id, ClaimsPrincipal session) => _github.ForLink(new(id, "display", "Person"), session, FactId.New());

    /// <summary>
    /// Vectors assume a verified adapter response. This test-only construction bypasses transport verification,
    /// not account policy, projections, decision guards, event storage or the Chronicle uniqueness constraint.
    /// </summary>
    /// <param name="issuer">The already-verified authority.</param>
    /// <param name="subject">The already-verified immutable subject.</param>
    /// <param name="operation">The accepted operation reference retained for retries.</param>
    /// <param name="session">The existing account session for a link ceremony.</param>
    /// <returns>The vector's adapter proof fixture.</returns>
    private protected VerifiedAuthentication Verified(UpstreamIssuer issuer, UpstreamSubject subject, FactId operation, ClaimsPrincipal? session = null)
    {
        _subjects.Add(new(operation.Value.ToString("D")));
        var proof = (VerifiedAuthentication)Activator.CreateInstance(
            typeof(VerifiedAuthentication),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [new ConnectionId(Guid.Parse("97373bfd-7d70-429e-a795-2147fddf8108")), issuer, subject, _clock.Now, operation],
            null)!;
        typeof(VerifiedAuthentication).GetProperty("SessionId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(proof, session?.FindFirstValue("sid") is { } sid ? new Cratis.Identity.Sessions.SessionId(sid) : null);
        return proof;
    }

    protected ClaimsPrincipal Session(AccountSubject account, bool resolver = false)
    {
        var principal = EventSourcedAccountPrincipal.For(account, "display", _clock);
        var identity = (ClaimsIdentity)principal.Identity;
        identity.RemoveClaim(identity.FindFirst("sid"));
        identity.AddClaim(new("sid", $"session-{account.Value}"));
        if (resolver)
        {
            identity.AddClaim(new("permission", "identity:resolve-links"));
        }

        return principal;
    }

    protected async Task<LinkId> Seed(AccountSubject account, UpstreamIssuer issuer, UpstreamSubject subject)
    {
        _subjects.Add(account);
        LinkId link = Guid.NewGuid();
        var result = await _events.EventLog.AppendMany(account.Value, [
            new AccountRegistrationRecorded(_clock.Now, FactId.New()),
            new UpstreamKeyLinkRecorded(link, Guid.NewGuid(), issuer, _digester.Digest(issuer, subject), Guid.NewGuid(), _clock.Now, FactId.New()),
            new UpstreamKeySubjectRecorded(link, issuer, subject)]);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("Could not seed the account fixture.");
        }

        await Refresh();
        return link;
    }

    protected async Task Refresh()
    {
        _states.Dispose();
        _reservations.Dispose();
        _profiles.Dispose();
        WireReaders();
        var types = typeof(AccountRegistrationRecorded).Assembly.GetTypes()
            .Where(type => type.IsDefined(typeof(EventTypeAttribute)) && !type.IsDefined(typeof(PublicAttribute)))
            .Select(_events.EventTypes.GetEventTypeFor).ToArray();
        foreach (var account in _subjects)
        {
            var events = await _events.EventLog.GetForEventSourceIdAndEventTypes(account.Value, types);
            await _states.ProcessAppendedEvents(events);
            await _reservations.ProcessAppendedEvents(events);
            await _profiles.ProcessAppendedEvents(events);
        }

        _ = _states.Instances;
        _ = _reservations.Instances;
        _ = _profiles.Instances;
    }

    private protected async Task<AccountSubject> SignIn(VerifiedAuthentication authentication, Email email, PersonalName name, Login login, AccountSubject? allocatedSubject = null)
    {
        await Refresh();
        var reservation = await _lookups.Find(_digester.Digest(authentication.IdpIssuer, authentication.Subject));
        var subject = reservation?.AccountSubject ?? allocatedSubject ?? AccountSubject.New();
        _subjects.Add(subject);
        var account = _profiles.InstanceForEventSourceId(subject.Value);
        var decision = new SignInDecision(subject, authentication, _digester.Digest(authentication.IdpIssuer, authentication.Subject), new(email, name, login), reservation, account, _clock.GetUtcNow());
        var result = new SignInWithUpstreamKey(authentication.OperationId).Handle(decision);
        if (result.TryGetError(out var refusal))
        {
            throw new AccountOperationRefused(refusal.Message);
        }

        await _store.Append(subject, await _store.Read(subject), result.AsT0.Item1.Select(item => item.Event));
        return result.AsT0.Item2;
    }

    AccountLinkState StateFor(string id) => _states.InstanceForEventSourceId(id) ?? new(id);

    void WireReaders()
    {
        // Standalone EventStoreForTesting cannot enumerate materialized rows without a silo. Feed its ACTUAL
        // appended events through Chronicle's read-model harness, retaining real writer/constraint/guard paths.
        _states = new();
        _reservations = new();
        _profiles = new();
        var reader = Substitute.For<IReadModels>();
        reader.GetInstances<UpstreamKeyLookup>(default).ReturnsForAnyArgs(_ => Task.FromResult(_reservations.Instances.Values));
        reader.GetInstanceById<AccountLinkState>(default(ReadModelKey)!, default).ReturnsForAnyArgs(call => Task.FromResult(StateFor(call.Arg<ReadModelKey>().Value)));
        _lookups = new(reader);
        var store = new AccountStore(_events, reader, _clock);
        _store = store;
        _linking = new(store, _lookups, _digester, Options.Create(_options), _clock);
    }
}
