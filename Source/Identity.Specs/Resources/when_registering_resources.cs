// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Resources.Registering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Resources;

public class when_registering_resources : given.resource_commands
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relative")]
    [InlineData("http://api.example")]
    [InlineData("file:///api")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user@api.example")]
    [InlineData("https://api.example?query")]
    [InlineData("https://api.example?")]
    [InlineData("https://api.example#fragment")]
    [InlineData("https://api.example#")]
    [InlineData(" https://api.example")]
    [InlineData("https://api.example/a//b")]
    [InlineData("https:\\api.example")]
    [InlineData("https://\u200d.example")]
    public void should_refuse_hostile_indicators(string? indicator) =>
        new RegisterResource(indicator, "api", "api", new("API")).Handle(_decision).IsT1.ShouldBeTrue();

    [Theory]
    [InlineData("", "api")]
    [InlineData("API", "api")]
    [InlineData("api--server", "api")]
    [InlineData("api-", "api")]
    [InlineData("api:other", "api")]
    [InlineData("api", "api:")]
    [InlineData("api", "Api")]
    [InlineData("api", "api_other")]
    [InlineData("api", "")]
    public void should_refuse_invalid_audiences_and_prefixes(string audience, string prefix) =>
        new RegisterResource("https://api.example", audience, prefix, new("API")).Handle(_decision).IsT1.ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("API\nLabel")]
    public void should_refuse_invalid_business_labels(string? label) =>
        new RegisterResource("https://api.example", "api", "api", label is null ? null : new(label)).Handle(_decision).IsT1.ShouldBeTrue();

    [Fact]
    void should_bound_all_registration_fields()
    {
        new RegisterResource($"https://api.example/{new string('a', 2048)}", "api", "api", new("API")).Handle(_decision).IsT1.ShouldBeTrue();
        new RegisterResource("https://api.example", new string('a', 65), "api", new("API")).Handle(_decision).IsT1.ShouldBeTrue();
        new RegisterResource("https://api.example", "api", new string('a', 65), new("API")).Handle(_decision).IsT1.ShouldBeTrue();
        new RegisterResource("https://api.example", "api", "api", new(new string('a', 257))).Handle(_decision).IsT1.ShouldBeTrue();
        new RegisterResource("https://api.example", "api", "api", null!).Handle(_decision).IsT1.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://API.EXAMPLE:443/", "https://api.example")]
    [InlineData("https://API.EXAMPLE/a/../b/", "https://api.example/b")]
    [InlineData("https://bücher.example", "https://xn--bcher-kva.example")]
    [InlineData("https://XN--BCHER-KVA.EXAMPLE.:443/", "https://xn--bcher-kva.example")]
    [InlineData("https://api.example./", "https://api.example")]
    [InlineData("https://api.example:444/", "https://api.example:444")]
    [InlineData("https://[2001:DB8::1]:443/", "https://[2001:db8::1]")]
    public void should_emit_only_an_internal_fact_on_a_new_uuid_stream(string indicator, string expected)
    {
        var command = new RegisterResource(indicator, "api", "api", new("API"));
        var events = command.Handle(_decision).AsT0.ToArray();
        events.Length.ShouldEqual(1);
        events[0].Event.ShouldBeOfExactType<ResourceRegistrationRecorded>();
        var fact = (ResourceRegistrationRecorded)events[0].Event;
        fact.ResourceIndicator.ShouldEqual(expected);
        fact.Audience.ShouldEqual("api");
        fact.DisplayName.Value.ShouldEqual("API");
        fact.FactId.Value.ShouldNotEqual(Guid.Empty);
        Guid.Parse(events[0].EventSourceId).Version.ShouldEqual(4);
        command.Handle(_decision).AsT0.Single().EventSourceId.ShouldNotEqual(events[0].EventSourceId);
    }

    [Theory]
    [InlineData("https://API.EXAMPLE:443/", "other", "other")]
    [InlineData("https://other.example", "api", "other")]
    [InlineData("https://other.example", "other", "api")]
    public void should_read_and_refuse_configuration_reservations(string indicator, string audience, string prefix)
    {
        var catalog = ConfiguredCatalog("https://api.example");
        var command = new RegisterResource(indicator, audience, prefix, new("Other"));
        command.Handle(command.Provide(new(catalog))).IsT1.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://bücher.example", "https://xn--bcher-kva.example")]
    [InlineData("https://xn--bcher-kva.example", "https://bücher.example")]
    [InlineData("https://BÜCHER.EXAMPLE.:443/", "https://xn--bcher-kva.example")]
    [InlineData("https://xn--bcher-kva.example", "https://BÜCHER.EXAMPLE.:443/")]
    [InlineData("https://API.EXAMPLE.:443/", "https://api.example")]
    [InlineData("https://api.example", "https://API.EXAMPLE.:443/")]
    public void should_refuse_configured_indicator_aliases_in_both_directions(string configured, string proposed)
    {
        var catalog = ConfiguredCatalog(configured);
        var command = new RegisterResource(proposed, "different", "different", new("Other"));
        command.Handle(command.Provide(new(catalog))).IsT1.ShouldBeTrue();
    }

    [Fact]
    void should_register_the_trusted_read_in_the_existing_resource_registration()
    {
        var services = new ServiceCollection();
        services.AddOptions<IdentityOptions>();
        services.AddResources();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ResourceRegistrationReads>().Read().Resources.ShouldBeEmpty();
    }

    static ResourceCatalog ConfiguredCatalog(string indicator) => new(Options.Create(new IdentityOptions
    {
        Resources = [new() { Audience = "api", Indicators = [indicator], Scopes = [new() { Name = "api:read" }] }]
    }));
}
