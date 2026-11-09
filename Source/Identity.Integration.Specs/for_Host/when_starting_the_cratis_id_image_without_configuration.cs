// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Identity.Integration.for_Host.given;

namespace Cratis.Identity.Integration.for_Host;

/// <summary>
/// CI-only container smoke: the image must refuse to start without its required configuration, and must not echo secrets while refusing.
/// </summary>
public class when_starting_the_cratis_id_image_without_configuration
{
    const string SensitiveValue = "synthetic-sensitive-value-for-smoke";

    /// <summary>Gets a value indicating whether CI explicitly opted into the container smoke.</summary>
    public static bool ImageSmokeEnabled => CratisIdImage.SmokeEnabled;

    [Fact(SkipUnless = nameof(ImageSmokeEnabled), Skip = "Container smoke is opt-in: set CI=true and CRATIS_ID_IMAGE_SMOKE_ENABLED=true with the image loaded locally.")]
    public async Task should_exit_non_zero_without_echoing_supplied_secrets()
    {
        var start = new ProcessStartInfo("docker") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "run", "--rm", "-e", "ASPNETCORE_ENVIRONMENT=Production", "-e", "Identity__GitHub__ClientSecret", "-e", "Identity__MongoDB__ConnectionString", CratisIdImage.Name })
        {
            start.ArgumentList.Add(argument);
        }

        // Only the secret-bearing settings are supplied; every other required setting is deliberately missing.
        start.Environment["Identity__GitHub__ClientSecret"] = SensitiveValue;
        start.Environment["Identity__MongoDB__ConnectionString"] = $"mongodb://user:{SensitiveValue}@localhost";

        using var process = Process.Start(start);
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        await process.WaitForExitAsync(timeout.Token);
        var logs = await output + await error;

        // 125-127 are docker's own failures (daemon, create or exec), not the image refusing to start.
        (process.ExitCode is 0 or 125 or 126 or 127).ShouldBeFalse();
        logs.Contains(SensitiveValue, StringComparison.Ordinal).ShouldBeFalse();
        logs.Contains("mongodb://", StringComparison.Ordinal).ShouldBeFalse();
    }
}
