// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.for_ProductTenantsCallback.given;

namespace Cratis.Identity.Accounts.for_DestroySubjectKeysStage;

public class when_key_destruction_fails_for_one_namespace_and_is_retried : Specification
{
    const string Healthy = "tenant-a";
    const string Flaky = "tenant-b";

    readonly Dictionary<string, int> _calls = new() { [Healthy] = 0, [Flaky] = 0 };
    CallbackLogger<DestroySubjectKeysStage> _logger;
    ErasureStageOutcome _first;
    ErasureStageOutcome _retry;

    void Establish()
    {
        var client = Substitute.For<IChronicleClient>();
        var eventStore = Substitute.For<IEventStore>();
        foreach (var @namespace in new[] { Healthy, Flaky })
        {
            var name = @namespace;
            var manager = Substitute.For<IPIIManager>();
            manager.DeleteEncryptionKeyFor(Arg.Any<EncryptionKeyIdentifier>()).Returns(_ =>
            {
                _calls[name]++;
                return name == Flaky && _calls[name] == 1
                    ? Task.FromException(new InvalidOperationException("kernel unavailable for subject 7d9d5c9e"))
                    : Task.CompletedTask;
            });
            var store = Substitute.For<IEventStore>();
            store.PII.Returns(manager);
            client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Is<EventStoreNamespaceName>(candidate => candidate.Value == name)).Returns(store);
        }

        _logger = new CallbackLogger<DestroySubjectKeysStage>();
        _stage = new DestroySubjectKeysStage(client, eventStore, _logger);
        _context = new ErasureContext(new("7d9d5c9e-0000-4000-8000-000000000001"), ErasureId.New(), [Healthy, Flaky]);
    }

    DestroySubjectKeysStage _stage;
    ErasureContext _context;

    async Task Because()
    {
        _first = await _stage.Execute(_context);
        _retry = await _stage.Execute(_context);
    }

    [Fact] void should_fail_the_first_attempt_with_the_allowlisted_reason() => _first.Reason.Value.ShouldEqual("erasure_key_destruction_failed");
    [Fact] void should_still_attempt_the_healthy_namespace_on_the_failing_run() => _calls[Healthy].ShouldEqual(2);
    [Fact] void should_succeed_on_retry() => _retry.Succeeded.ShouldBeTrue();
    [Fact] void should_cover_every_namespace_on_retry() => _retry.Covered.ShouldContainOnly(Healthy, Flaky);
    [Fact] void should_log_the_namespace_and_exception_type() => _logger.Messages.ShouldContainOnly($"Subject key destruction failed in namespace {Flaky}: {typeof(InvalidOperationException).FullName}");
    [Fact] void should_not_log_the_subject_or_message_content() => _logger.Messages.Exists(message => message.Contains("7d9d5c9e")).ShouldBeFalse();
}
