// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Integration.for_Host.given;

namespace Cratis.Identity.Integration.for_Host;

/// <summary>
/// CI-only container smoke: starts the loaded image against provisioned throwaway stores, Chronicle and keys.
/// </summary>
public class when_starting_the_cratis_id_image
{
    /// <summary>Gets a value indicating whether CI explicitly opted into the container smoke.</summary>
    public static bool ImageSmokeEnabled => CratisIdImage.SmokeEnabled;

    [Fact(SkipUnless = nameof(ImageSmokeEnabled), Skip = "Container smoke is opt-in: set CI=true and CRATIS_ID_IMAGE_SMOKE_ENABLED=true with the image loaded locally.")]
    public async Task should_serve_liveness_and_reconciled_readiness()
    {
        await using var fixture = new CratisIdImageFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        using var http = new HttpClient { BaseAddress = fixture.BaseAddress };
        using var live = await http.GetAsync("/healthz", TestContext.Current.CancellationToken);
        live.StatusCode.ShouldEqual(HttpStatusCode.OK);
        using var ready = await http.GetAsync("/readyz", TestContext.Current.CancellationToken);
        ready.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }
}
