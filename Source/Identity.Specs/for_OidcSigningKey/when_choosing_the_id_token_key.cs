// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_OidcSigningKey;

public class when_choosing_the_id_token_key : Specification
{
    List<X509Certificate2> _certificates = [];
    RealmKey _current;
    RealmKey _older;
    RealmKey _expired;
    RealmKey _prepublished;
    RealmKey _elliptic;
    DateTimeOffset _now;

    void Establish()
    {
        _now = KeyMaterial.Now;
        var activated = _now - KeyEligibility.PrePublication - TimeSpan.FromHours(1);
        _current = Key("rsa-current", KeyMaterial.Rsa(activated, _now.AddDays(1)));
        _older = Key("rsa-older", KeyMaterial.Rsa(activated.AddDays(-1), _now.AddDays(1)));
        _expired = Key("rsa-expired", KeyMaterial.Rsa(activated, _now));
        _prepublished = Key("rsa-future", KeyMaterial.Rsa(_now, _now.AddDays(1)));
        _elliptic = Key("ec-current", KeyMaterial.Ec(_now.AddHours(-2), _now.AddDays(1)), SecurityAlgorithms.EcdsaSha256);
    }

    [Fact] void should_choose_the_newest_eligible_rsa() => OidcSigningKey.Select([_older, _current, _expired, _prepublished, _elliptic], _now).ShouldEqual(_current);
    [Fact] void should_not_fall_back_to_an_expired_key() => OidcSigningKey.Select([_expired], _now).ShouldBeNull();
    [Fact] void should_not_fall_back_to_a_prepublication_key() => OidcSigningKey.Select([_prepublished], _now).ShouldBeNull();
    [Fact] void should_not_fall_back_to_ec() => OidcSigningKey.Select([_elliptic], _now).ShouldBeNull();
    [Fact] void should_break_activation_ties_by_ordinal_kid() => OidcSigningKey.Select([_current with { Kid = "b" }, _current with { Kid = "a" }], _now)!.Kid.ShouldEqual("a");

    void Destroy()
    {
        foreach (var certificate in _certificates) certificate.Dispose();
    }

    RealmKey Key(string kid, PemPair material, string algorithm = SecurityAlgorithms.RsaSha256)
    {
        var certificate = X509Certificate2.CreateFromPem(material.Certificate, material.Key);
        _certificates.Add(certificate);
        return new(kid, kid, certificate, KeyPurpose.Signing, algorithm);
    }
}
