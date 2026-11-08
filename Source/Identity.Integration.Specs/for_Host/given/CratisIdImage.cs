// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Host.given;

/// <summary>
/// Names the locally available Cratis ID image under smoke test and the opt-in that enables the container smoke specs.
/// </summary>
public static class CratisIdImage
{
    /// <summary>Gets the image reference; CI loads <c language="text">cratis-id:ci</c> into the local daemon without publishing it.</summary>
    public static string Name => Environment.GetEnvironmentVariable("CRATIS_ID_IMAGE_SMOKE_IMAGE") ?? "cratis-id:ci";

    /// <summary>Gets a value indicating whether CI explicitly opted into the container smoke specs.</summary>
    public static bool SmokeEnabled => Environment.GetEnvironmentVariable("CI") == "true" &&
        Environment.GetEnvironmentVariable("CRATIS_ID_IMAGE_SMOKE_ENABLED") == "true";
}
