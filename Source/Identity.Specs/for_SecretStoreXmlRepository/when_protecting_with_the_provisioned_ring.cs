// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.for_SecretStoreXmlRepository;

public class when_protecting_with_the_provisioned_ring : given.a_data_protection_ring
{
    string _roundTripped;
    string _protected;

    void Establish() => Open();

    void Because()
    {
        var protector = Provider().CreateProtector("spec");
        _protected = protector.Protect("payload");
        _roundTripped = Provider().CreateProtector("spec").Unprotect(_protected);
    }

    [Fact] void should_unprotect_what_was_protected() => _roundTripped.ShouldEqual("payload");
}
