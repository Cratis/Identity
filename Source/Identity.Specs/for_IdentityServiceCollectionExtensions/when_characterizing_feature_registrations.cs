// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_characterizing_feature_registrations : given.registered_features
{
    string[] _enabled;
    string[] _disabled;

    /// <summary>
    /// Captured by running this projection against b17ded8's original, unextracted registration.
    /// Unlike e3fcaff, b17ded8 includes the merge replacing AccountStore/UpstreamKeyLinking with
    /// LinkCeremony, ILinkCeremony and AccountLinking. The initial baseline edit was analyzer-required method chaining.
    /// D3's AddClients(configuration) adds ClientSecretDigester, ClientRegistrationReads, ClientSecretOptions binding,
    /// two validators and startup validation. Only those descriptors and repeated entries were added during the merge.
    /// </summary>
    static string[] ExpectedEnabled => ReadBaseline("enabled");
    static string[] ExpectedDisabled => ReadBaseline("disabled");

    void Because()
    {
        _enabled = Snapshot(enabled_services, enabled_provider);
        _disabled = Snapshot(disabled_services, disabled_provider);
    }

    [Fact]
    void should_preserve_enabled_registrations_and_order_sensitive_subsequences() => _enabled.ShouldEqual(ExpectedEnabled);

    [Fact]
    void should_preserve_disabled_registrations_and_order_sensitive_subsequences() => _disabled.ShouldEqual(ExpectedDisabled);

    static string[] ReadBaseline(string variant)
    {
        using var stream = typeof(when_characterizing_feature_registrations).Assembly.GetManifestResourceStream(
            $"Cratis.Identity.for_IdentityServiceCollectionExtensions.Baselines.device-{variant}.txt");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith("//", StringComparison.Ordinal))
            .ToArray();
    }
}
