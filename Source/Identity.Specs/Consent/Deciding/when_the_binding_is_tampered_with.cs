// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_the_binding_is_tampered_with : given.an_activation
{
    ConsentRequest?[] _results;
    string?[] _codes;

    async Task Because()
    {
        var hostile = new[]
        {
            binding with { Realm = "realm-b" }, binding with { Client = "client-b" },
            binding with { Account = "00000000-0000-4000-8000-000000000002" }, binding with { Tenant = "tenant-b" },
            binding with { Resource = "resource-b" }, binding with { Scopes = "openid admin" }, binding with { Session = "session-b" }
        };
        _results = await Task.WhenAll(hostile.Select(_ => activation.Decide(request.Id, _, true, CancellationToken.None)));
        await Approve();
        await Resume();
        _codes = await Task.WhenAll(hostile.Select(_ => activation.ClaimCode(request.Id, _, CancellationToken.None)));
    }

    [Fact] void should_reject_every_mismatched_binding() => _results.All(_ => _ is null).ShouldBeTrue();
    [Fact] void should_deny_every_mismatched_code_claim() => _codes.All(_ => _ is null).ShouldBeTrue();
    [Fact] void should_not_amplify_evidence() => evidence.Appends.ShouldEqual(1);
}
