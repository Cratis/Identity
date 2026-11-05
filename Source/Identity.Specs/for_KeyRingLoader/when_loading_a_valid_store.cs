// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_KeyRingLoader;

public class when_loading_a_valid_store : given.a_secret_store
{
    PemPair _ec;
    KeyRing _again;

    void Establish()
    {
        _ec = KeyMaterial.Ec(KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddDays(30));
        _files.Pair("signing", "b", _ec);
    }

    void Because()
    {
        Load();
        _again = KeyRingLoader.Load(_files);
    }

    [Fact] void should_load_both_signing_keys() => _ring.Signing.Count.ShouldEqual(2);
    [Fact] void should_load_the_encryption_key() => _ring.Encryption.Count.ShouldEqual(1);
    [Fact] void should_load_the_data_protection_key() => _ring.DataProtection.Count.ShouldEqual(1);
    [Fact] void should_use_the_thumbprint_as_kid() => _ring.Signing.Single(_ => _.Name == "b").Kid.ShouldEqual(KeyMaterial.KeyThumbprint(_ec));
    [Fact] void should_sign_rsa_keys_with_rs256() => _ring.Signing.Single(_ => _.Name == "a").Algorithm.ShouldEqual(SecurityAlgorithms.RsaSha256);
    [Fact] void should_sign_ec_keys_with_es256() => _ring.Signing.Single(_ => _.Name == "b").Algorithm.ShouldEqual(SecurityAlgorithms.EcdsaSha256);
    [Fact] void should_encrypt_with_rsa_oaep() => _ring.Encryption.Single().Algorithm.ShouldEqual(SecurityAlgorithms.RsaOAEP);
    [Fact] void should_keep_the_private_key() => _ring.Signing.All(_ => _.Certificate.HasPrivateKey).ShouldBeTrue();
    [Fact] void should_keep_the_kid_stable_across_reloads() => _again.Signing.Select(_ => _.Kid).ShouldEqualInAnyOrder(_ring.Signing.Select(_ => _.Kid));
}
