// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_characterizing_feature_registrations : given.registered_features
{
    string[] _enabled;
    string[] _disabled;
    string[] _framework;

    /// <summary>
    /// Captured by running this projection against b17ded8's original, unextracted registration.
    /// Unlike e3fcaff, b17ded8 includes the merge replacing AccountStore/UpstreamKeyLinking with
    /// LinkCeremony, ILinkCeremony and AccountLinking. The initial baseline edit was analyzer-required method chaining.
    /// D3's AddClients(configuration) adds ClientSecretDigester, ClientSecretOptions binding,
    /// two validators and startup validation. Only those descriptors and repeated entries were added during the merge.
    /// Connected-app readers and revocation services are always available, independently of device grants and DPoP.
    /// </summary>
    static string[] ExpectedEnabled => ReadBaseline("enabled");
    static string[] ExpectedDisabled => ReadBaseline("disabled");

    void Because()
    {
        _enabled = Snapshot(enabled_services, enabled_provider);
        _disabled = Snapshot(disabled_services, disabled_provider);
        var services = given.FrameworkRegistrationSnapshot.Register();
        using var provider = services.BuildServiceProvider();
        _framework = Snapshot(services, provider).Where(given.FrameworkRegistrationSnapshot.IsRuntimeOwned).ToArray();
    }

    [Fact]
    void should_preserve_enabled_registrations_and_order_sensitive_subsequences() =>
        given.FrameworkRegistrationSnapshot.IdentityRegistrations(_enabled).ShouldEqual(ExpectedEnabled);

    [Fact]
    void should_preserve_disabled_registrations_and_order_sensitive_subsequences() =>
        given.FrameworkRegistrationSnapshot.IdentityRegistrations(_disabled).ShouldEqual(ExpectedDisabled);

    [Fact]
    void should_characterize_nonempty_framework_registrations() => _framework.ShouldNotBeEmpty();

    [Fact]
    void should_match_isolated_framework_registrations_when_enabled() =>
        _enabled.Where(given.FrameworkRegistrationSnapshot.IsRuntimeOwned).ToArray().ShouldEqual(_framework);

    [Fact]
    void should_match_isolated_framework_registrations_when_disabled() =>
        _disabled.Where(given.FrameworkRegistrationSnapshot.IsRuntimeOwned).ToArray().ShouldEqual(_framework);

    static string[] ReadBaseline(string variant)
    {
        using var stream = typeof(when_characterizing_feature_registrations).Assembly.GetManifestResourceStream(
            $"Cratis.Identity.for_IdentityServiceCollectionExtensions.Baselines.device-{variant}.txt");
        using var reader = new StreamReader(stream);
        var baseline = given.FrameworkRegistrationSnapshot.IdentityRegistrations(reader.ReadToEnd()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith("//", StringComparison.Ordinal))
            .ToArray());
        string[] connectedApps =
        [
            "descriptor|Cratis.Identity.Authorization.ConnectedApps.ConnectedAppReader|Scoped|factory",
            "descriptor|Cratis.Identity.Authorization.ConnectedApps.ConnectedAppRevocation|Scoped|factory"
        ];
        return baseline.TakeWhile(entry => entry.StartsWith("descriptor|", StringComparison.Ordinal))
            .Concat(connectedApps)
            .Order(StringComparer.Ordinal)
            .Concat(baseline.SkipWhile(entry => entry.StartsWith("descriptor|", StringComparison.Ordinal)))
            .ToArray();
    }
}
