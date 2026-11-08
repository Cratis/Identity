// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_class_coverage_is_replayed_in_a_new_epoch : given.a_recovery_pass
{
    static readonly RestrictionClass[] _coveredClasses =
    [
        RestrictionClass.Grants,
        RestrictionClass.Tokens,
        RestrictionClass.Authorizations,
        RestrictionClass.Consent,
        RestrictionClass.Keys,
        RestrictionClass.ClientSecrets
    ];

    RestrictionClass[] _deniedBeforeReplay = [];
    RestrictionClass[] _coveredBeforeAdvance = [];
    bool _acceptedBeforeAdvance;
    RestrictionClass[] _deniedAfterAdvance = [];
    bool _acceptedAfterAdvance;
    long _previousEpoch;

    void Establish() => _custody = _custody with { Epoch = 1, ClearedEpoch = -1 };

    async Task Because()
    {
        _gate.ObserveEpoch(_custody.Epoch);
        _deniedBeforeReplay = DeniedClasses();
        await Run();
        _previousEpoch = _gate.Epoch;
        _coveredBeforeAdvance = _coveredClasses.Where(_gate.Allows).ToArray();
        _acceptedBeforeAdvance = _gate.Accepts(_previousEpoch);

        _custody = _custody with { Epoch = _previousEpoch + 1 };
        _gate.ObserveEpoch(_custody.Epoch);
        _deniedAfterAdvance = DeniedClasses();
        _acceptedAfterAdvance = _gate.Accepts(_previousEpoch);
        await Run();
    }

    [Fact] void should_deny_each_class_before_its_coverage_is_replayed() => _deniedBeforeReplay.ShouldContainOnly(_coveredClasses);
    [Fact] void should_have_covered_each_class_before_the_epoch_advanced() => _coveredBeforeAdvance.ShouldContainOnly(_coveredClasses);
    [Fact] void should_have_accepted_consent_in_the_previous_epoch() => _acceptedBeforeAdvance.ShouldBeTrue();
    [Fact] void should_deny_each_class_when_the_epoch_advances() => _deniedAfterAdvance.ShouldContainOnly(_coveredClasses);
    [Fact] void should_deny_previous_consent_when_the_epoch_advances() => _acceptedAfterAdvance.ShouldBeFalse();
    [Fact] void should_restore_class_coverage_after_replay() => _coveredClasses.Where(_gate.Allows).ShouldContainOnly(_coveredClasses);
    [Fact] void should_not_resurrect_previous_consent_after_replay() => _gate.Accepts(_previousEpoch).ShouldBeFalse();
    [Fact] void should_accept_new_consent_after_replay() => _gate.Accepts(_gate.Epoch).ShouldBeTrue();
    [Fact] void should_still_require_operator_attestation_after_replay() => _gate.Ready.ShouldBeFalse();

    RestrictionClass[] DeniedClasses() => _coveredClasses.Where(restriction => Catch.Exception(() => _gate.Demand(restriction)) is RevocationPending).ToArray();
}
