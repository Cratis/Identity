// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_the_route_prefix_is_hostile : given.an_identity_host
{
    public static TheoryData<string> Prefixes => new()
    {
        "id", "/id/", "//evil", "/a/../b", "/.", "/a/./b", "/%2e%2e", "/a?x=1", "/a#f", "/a\\b",
        "/.well-known", "/.well-known/nested", "/signin-github", "/signin-github/nested", "/healthz", "/readyz/child",
        "/HEALTHZ", "/a b", "/a\tb", "/a\nb", "/idé", "/" + new string('a', 128)
    };

    [Theory]
    [MemberData(nameof(Prefixes))]
    async Task should_refuse_the_prefix_during_host_registration(string prefix)
    {
        var builder = CreateBuilder(prefix);
        var exception = Catch.Exception(() => builder.AddCratisIdentity());
        exception.ShouldBeOfExactType<ArgumentException>();
        await Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(Prefixes))]
    async Task should_refuse_the_prefix_before_mapping_any_endpoints(string prefix)
    {
        var builder = CreateBuilder();
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        var exception = Catch.Exception(() => app.MapCratisIdentity(options => options.RoutePrefix = prefix));
        exception.ShouldBeOfExactType<ArgumentException>();
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ShouldBeEmpty();
    }
}
