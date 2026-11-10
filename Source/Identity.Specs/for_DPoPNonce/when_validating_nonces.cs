// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.DPoP;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.for_DPoPNonce;

public class when_validating_nonces : Specification
{
    const string Origin = "https://identity.example";
    Clock _clock;
    IDataProtectionProvider _ring;
    DPoPNonce _issuer;
    DPoPNonce _replica;
    string _nonce;

    void Establish()
    {
        _clock = new Clock();
        _ring = new EphemeralDataProtectionProvider();
        _issuer = new DPoPNonce(_ring, _clock);
        _replica = new DPoPNonce(_ring, _clock);
        _nonce = _issuer.Issue(Origin, "synthetic-jkt");
    }

    [Fact] void should_accept_on_another_replica_with_the_same_ring() => _replica.Validate(_nonce, Origin, "synthetic-jkt").ShouldBeTrue();
    [Fact] void should_refuse_the_wrong_origin() => _replica.Validate(_nonce, "https://other.example", "synthetic-jkt").ShouldBeFalse();
    [Fact] void should_refuse_the_wrong_key() => _replica.Validate(_nonce, Origin, "other-key").ShouldBeFalse();
    [Fact] void should_refuse_another_ring() => new DPoPNonce(new EphemeralDataProtectionProvider(), _clock).Validate(_nonce, Origin, "synthetic-jkt").ShouldBeFalse();
    [Fact] void should_refuse_tampering() => _replica.Validate("tampered" + _nonce, Origin, "synthetic-jkt").ShouldBeFalse();
    [Fact] void should_refuse_an_absent_nonce() => _replica.Validate(null, Origin, "synthetic-jkt").ShouldBeFalse();

    [Theory]
    [InlineData(299, true)]
    [InlineData(300, true)]
    [InlineData(329, true)]
    [InlineData(330, false)]
    [InlineData(-30, true)]
    [InlineData(-31, false)]
    void should_accept_only_the_overlapping_time_window(int seconds, bool accepted)
    {
        var replica = new DPoPNonce(_ring, new Clock { Now = _clock.Now.AddSeconds(seconds) });
        replica.Validate(_nonce, Origin, "synthetic-jkt").ShouldEqual(accepted);
    }

    sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = DateTimeOffset.FromUnixTimeSeconds(2000000000);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
