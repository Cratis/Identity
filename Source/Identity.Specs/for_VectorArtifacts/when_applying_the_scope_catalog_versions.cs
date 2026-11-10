// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_VectorArtifacts;

public class when_applying_the_scope_catalog_versions : Specification
{
    [Theory]
    [InlineData("v2.0.0", "direct-api")]
    [InlineData("v2.1.0", "direct-mcp")]
    public void should_apply_the_receiver_audience_and_scope_catalog_cases(string version, string audience)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Vectors");
        var directory = version == "v2.0.0" ? root : Path.Combine(root, version);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "arc-scope-catalog-v1", "catalog-and-policy.json")));
        var catalog = new ResourceCatalog(Options.Create(new IdentityOptions
        {
            Resources = [new ResourceServerOptions
            {
                Audience = audience,
                Indicators = ["https://direct.example.test/mcp"],
                Scopes = [new() { Name = "direct:read" }, new() { Name = "direct:write" }]
            }]
        }));
        var cases = document.RootElement.GetProperty("given").GetProperty("vectors").EnumerateArray()
            .Where(vector => vector.GetProperty("when").TryGetProperty("receiver", out var receiver) && receiver.GetString() == "https://direct.example.test/mcp" &&
                !vector.GetProperty("given").TryGetProperty("productRole", out _)).ToArray();
        cases.Length.ShouldEqual(3);
        foreach (var vector in cases)
        {
            var given = vector.GetProperty("given");
            var receiver = catalog.FindByIndicator(vector.GetProperty("when").GetProperty("receiver").GetString());
            var authorized = receiver.Audience == given.GetProperty("audience").GetString() &&
                given.GetProperty("scopes").EnumerateArray().All(scope => receiver.Owns(scope.GetString()));
            authorized.ShouldEqual(vector.GetProperty("then").GetProperty("authorized").GetBoolean());
        }
    }
}
