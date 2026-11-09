// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

/// <summary>Second step: pass-through only. Identity does not issue DPoP-bound tokens yet.</summary>
public class when_a_validated_token_carries_confirmation : given.a_validated_access_token
{
    void Establish()
    {
        _context.GenericTokenPrincipal.SetClaim("cnf", new JsonObject { ["jkt"] = "validated-thumbprint" }.ToJsonString());
        _context.Request["cnf"] = new JsonObject { ["jkt"] = "request-thumbprint" }.ToJsonString();
    }

    async Task Because() => await Populate();

    [Fact]
    void should_pass_through_the_validated_thumbprint_only()
    {
        var thumbprint = _context.Confirmation["jkt"]?.GetValue<string>();
        thumbprint.ShouldEqual("validated-thumbprint");
    }
}
