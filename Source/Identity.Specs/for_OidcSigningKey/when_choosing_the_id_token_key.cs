// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_OidcSigningKey;

public class when_choosing_the_id_token_key : Specification
{
    X509Certificate2 _rsa;
    X509Certificate2 _ec;
    RealmKey _current;
    RealmKey _older;
    RealmKey _expired;
    RealmKey _prepublished;
    RealmKey _elliptic;
    DateTimeOffset _now;

    void Establish()
    {
        _now = KeyMaterial.Now;
        var rsa = KeyMaterial.CurrentRsa();
        var ec = KeyMaterial.Ec(_now.AddHours(-2), _now.AddDays(1));
        _rsa = X509Certificate2.CreateFromPem(rsa.Certificate, rsa.Key);
        _ec = X509Certificate2.CreateFromPem(ec.Certificate, ec.Key);
        _current = new("rsa-current", KeyPurpose.Signing, _rsa, SecurityAlgorithms.RsaSha256, _now - KeyEligibility.PrePublication - TimeSpan.FromHours(1), _now.AddDays(1));
        _older = _current with { Kid = "rsa-older", NotBefore = _current.NotBefore.AddDays(-1) };
        _expired = _current with { Kid = "rsa-expired", NotAfter = _now };
        _prepublished = _current with { Kid = "rsa-future", NotBefore = _now };
        _elliptic = _current with { Kid = "ec-current", Certificate = _ec, Algorithm = SecurityAlgorithms.EcdsaSha256 };
    }

    [Fact] void should_choose_the_newest_eligible_rsa() => OidcSigningKey.Select([_older, _current, _expired, _prepublished, _elliptic], _now).ShouldEqual(_current);
    [Fact] void should_not_fall_back_to_an_expired_key() => OidcSigningKey.Select([_expired], _now).ShouldBeNull();
    [Fact] void should_not_fall_back_to_a_prepublication_key() => OidcSigningKey.Select([_prepublished], _now).ShouldBeNull();
    [Fact] void should_not_fall_back_to_ec() => OidcSigningKey.Select([_elliptic], _now).ShouldBeNull();
    [Fact] void should_break_activation_ties_by_ordinal_kid() => OidcSigningKey.Select([_current with { Kid = "b" }, _current with { Kid = "a" }], _now)!.Kid.ShouldEqual("a");

    void Destroy()
    {
        _rsa.Dispose();
        _ec.Dispose();
    }
}
