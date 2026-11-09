// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Replay.given;
using Cratis.Identity.Revocation;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Replay.for_EventLog;

public class when_scanning_the_event_log_for_secrets : Accounts.SigningIn.given.a_sign_in_decision
{
    readonly PublicationReplay _replay = new();
    readonly List<string> _serialized = [];
    Type[] _registered;
    int _sourceCount;
    int _outboxCount;

    async Task Because()
    {
        var log = _replay.Store.GetEventSequence(EventSequenceId.Log);
        _registered = _replay.Store.EventTypes.AllClrTypes.Where(given.EventRegistry.Types.Contains).ToArray();
        foreach (var type in _registered) await log.Append(_subject.Value, given.EventSeeder.Seed(type));

        // Exercise the real account decision path; tokens are deliberately not part of its verified input.
        _readModels.GetInstances<UpstreamKeyLookup>(default).ReturnsForAnyArgs(Task.FromResult<IEnumerable<UpstreamKeyLookup>>([]));
        _account = null!;
        var accounts = Substitute.For<IAccountSignIn>();
        accounts.SignIn(Arg.Any<VerifiedAuthentication>(), Arg.Any<ObservedProfile>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _verified = new VerifiedUpstreamSignIn();
            _verified.Set(call.Arg<VerifiedAuthentication>(), call.Arg<ObservedProfile>(), CancellationToken.None);
            _command = new(call.Arg<VerifiedAuthentication>().OperationId);
            await Decide();
            foreach (var @event in _events) await log.Append(_subject.Value, @event);
            return new Account(_subject, "Person", "profile@example.test", "42", "synthetic-login", AccountStatus.Active, []);
        });
        await given.UpstreamTicket.SignIn(accounts, _clock);

        var fences = new RevocationSpecs.SharedFenceStore();
        await fences.Commit("scan-grant", _subject.Value, RevocationReason.RefreshTokenReused, CancellationToken.None);
        var state = await fences.Read("scan-grant", CancellationToken.None);
        foreach (var entry in state.Outbox) await new ChronicleRevocationFactWriter(_replay.Store).Append(entry, CancellationToken.None);
        await _replay.Replay(1);
        await _replay.Replay(1000);
        var source = await PublicationReplay.Read(log);
        var outbox = await _replay.Published();
        _sourceCount = source.Length;
        _outboxCount = outbox.Length;
        foreach (var @event in source.Concat(outbox))
        {
            _serialized.Add(JsonSerializer.Serialize(new { eventType = @event.Content.GetType().FullName, content = @event.Content, context = @event.Context }));
        }
    }

    [Fact] void should_cover_the_complete_registered_identity_event_surface() => _registered.OrderBy(type => type.FullName).ShouldEqual(given.EventRegistry.Types.OrderBy(type => type.FullName));
    [Fact] void should_scan_a_nonempty_source_log() => (_sourceCount > given.EventRegistry.Types.Length).ShouldBeTrue();
    [Fact] void should_exercise_the_real_sign_in_decision() => _refusal.ShouldBeNull();
    [Fact] void should_append_the_real_sign_in_decision() => _events.ShouldNotBeEmpty();
    [Fact] void should_scan_every_published_contract() => _outboxCount.ShouldEqual(6);
    [Fact] void should_find_no_secrets_in_content_or_envelopes() => given.EventLogScanner.Findings(_serialized, given.Secrets.All).ShouldBeEmpty();
    void Destroy() => _replay.Dispose();
}
