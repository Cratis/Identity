// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_EndSessionServiceCollectionExtensions;

public class when_configuring_session_limits : Specification
{
    [Theory]
    [InlineData(null, 86400)]
    [InlineData("02:00:00", 7200)]
    public void should_bind_the_public_and_validator_options_without_changing_the_cookie_key(string? maximumAge, int seconds)
    {
        var section = Configuration(maximumAge).GetSection(IdentityOptions.SectionName);
        var identity = section.Get<IdentityOptions>()!;
        var services = new ServiceCollection();
        services.AddEndSession(section);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        var limits = provider.GetRequiredService<IOptions<EndSessionOptions>>().Value;
        limits.EndSessionHintMaxAge.ShouldEqual(TimeSpan.FromSeconds(seconds));
        identity.Sessions.EndSessionHintMaxAge.ShouldEqual(limits.EndSessionHintMaxAge);
        identity.SessionLifetime.ShouldEqual(TimeSpan.FromMinutes(30));
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    public void should_refuse_non_positive_hint_ages_at_startup(string maximumAge)
    {
        var services = new ServiceCollection();
        services.AddEndSession(Configuration(maximumAge).GetSection(IdentityOptions.SectionName));
        using var provider = services.BuildServiceProvider();
        var exception = Catch.Exception(provider.GetRequiredService<IStartupValidator>().Validate);
        exception.ShouldBeOfExactType<OptionsValidationException>();
        exception.Message.ShouldContain("Identity:Sessions:EndSessionHintMaxAge must be positive.");
    }

    static IConfiguration Configuration(string? maximumAge)
    {
        var values = new Dictionary<string, string?> { ["Identity:SessionLifetime"] = "00:30:00" };
        if (maximumAge is not null)
        {
            values["Identity:Sessions:EndSessionHintMaxAge"] = maximumAge;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
