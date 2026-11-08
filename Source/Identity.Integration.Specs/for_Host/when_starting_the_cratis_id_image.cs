// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Cratis.Identity.Integration.for_Host;

/// <summary>CI-only container smoke scaffold; its external stores and test realm secrets must be provisioned first.</summary>
public class when_starting_the_cratis_id_image
{
    /// <summary>Gets a value indicating whether CI explicitly opted into the provisioned image fixture.</summary>
    public static bool ImageSmokeEnabled => Environment.GetEnvironmentVariable("CI") == "true" &&
        Environment.GetEnvironmentVariable("CRATIS_ID_IMAGE_SMOKE_ENABLED") == "true";

    [Fact(SkipUnless = nameof(ImageSmokeEnabled))]
    public async Task should_serve_liveness_and_reconciled_readiness()
    {
        var configurationDirectory = Environment.GetEnvironmentVariable("CRATIS_ID_IMAGE_SMOKE_CONFIGURATION_DIRECTORY");
        string.IsNullOrWhiteSpace(configurationDirectory).ShouldBeFalse();
        await using var container = new ContainerBuilder(Environment.GetEnvironmentVariable("CRATIS_ID_IMAGE_SMOKE_IMAGE") ?? "cratis-id:ci")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            .WithEnvironment("Identity__Hosting__Preset", "hosted-shared")
            .WithBindMount(configurationDirectory, "/configuration", AccessMode.ReadOnly)
            .WithCommand("--contentRoot", "/configuration")
            .WithPortBinding(8080, true)
            .WithCreateParameterModifier(parameters =>
            {
                foreach (var binding in parameters.HostConfig.PortBindings["8080/tcp"])
                {
                    binding.HostIP = "127.0.0.1";
                }
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(8080).ForPath("/readyz"), wait => wait.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();
        await container.StartAsync(TestContext.Current.CancellationToken);
        using var http = new HttpClient { BaseAddress = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(8080)}") };
        using var live = await http.GetAsync("/healthz", TestContext.Current.CancellationToken);
        live.StatusCode.ShouldEqual(HttpStatusCode.OK);
        using var ready = await http.GetAsync("/readyz", TestContext.Current.CancellationToken);
        ready.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }
}
