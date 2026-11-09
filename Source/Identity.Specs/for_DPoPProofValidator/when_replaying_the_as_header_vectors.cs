// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.DPoP;

namespace Cratis.Identity.for_DPoPProofValidator;

public class when_replaying_the_as_header_vectors : given.a_signed_proof
{
    [Theory]
    [InlineData("alg-none", "alg", DPoPFailure.UnsupportedAlgorithm)]
    [InlineData("alg-hs256", "alg", DPoPFailure.UnsupportedAlgorithm)]
    internal void should_enforce_the_shared_as_header_rules(string vector, string member, DPoPFailure expected)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Vectors", "identity-dpop-v1", vector + ".json")));
        var value = fixture.RootElement.GetProperty("given").GetProperty("proof").GetProperty("header").GetProperty(member).GetString();
        WithHeader(member, value).ShouldEqual(expected);
    }
}
