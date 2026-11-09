// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.Protocol.Stores;

public class when_reading_configured_clients : given.a_configuration_catalog
{
    readonly List<OpenIddictApplicationDescriptor> _actual = [];
    bool _correct;
    bool _wrong;
    bool _publicSecret;
    object _unknown;
    bool _loopback;
    bool _localhost;

    async Task Because()
    {
        await foreach (var application in Applications.ListAsync(null, null, CancellationToken.None))
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await Manager.PopulateAsync(descriptor, application);
            _actual.Add(descriptor);
        }
        var confidential = await Manager.FindByClientIdAsync("spec-api");
        _correct = await Manager.ValidateClientSecretAsync(confidential, Secret);
        _wrong = await Manager.ValidateClientSecretAsync(confidential, "wrong-spec-secret");
        _unknown = await Manager.FindByClientIdAsync("unknown-client");
        var cli = await Manager.FindByClientIdAsync("cratis-cli");
        _publicSecret = await Manager.ValidateClientSecretAsync(cli, Secret);
        _loopback = await Manager.ValidateRedirectUriAsync(cli, "http://127.0.0.1:49152/callback");
        _localhost = await Manager.ValidateRedirectUriAsync(cli, "http://localhost:49152/callback");
    }

    [Fact] void should_authenticate_the_configured_secret_using_the_real_manager() => _correct.ShouldBeTrue();
    [Fact] void should_reject_the_wrong_secret_using_the_real_manager() => _wrong.ShouldBeFalse();
    [Fact] void should_not_authenticate_an_unknown_client() => _unknown.ShouldBeNull();
    [Fact] void should_not_accept_a_secret_for_the_public_cli() => _publicSecret.ShouldBeFalse();
    [Fact] void should_keep_native_loopback_port_matching() => _loopback.ShouldBeTrue();
    [Fact] void should_keep_refusing_localhost() => _localhost.ShouldBeFalse();
    [Fact] void should_return_every_declared_client() => _actual.Count.ShouldEqual(Descriptors.All().Count());
    [Fact] void should_not_log_either_attempted_secret() => Logs.Any(_ => _.Contains(Secret, StringComparison.Ordinal) || _.Contains("wrong-spec-secret", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_return_the_clear_secret() => _actual.Exists(_ => _.ClientSecret == Secret).ShouldBeFalse();
    [Fact] void should_store_a_hash_for_the_introspection_client() => string.IsNullOrEmpty(_actual.Single(_ => _.ClientId == "spec-api").ClientSecret).ShouldBeFalse();
    [Fact] void should_leave_the_public_client_secret_empty() => _actual.Single(_ => _.ClientId == "cratis-cli").ClientSecret.ShouldBeNull();

    [Fact]
    void should_return_the_same_descriptors_except_for_secret_hashing()
    {
        foreach (var expected in Descriptors.All())
        {
            var actual = _actual.Single(_ => _.ClientId == expected.ClientId);
            actual.ApplicationType.ShouldEqual(expected.ApplicationType);
            actual.ClientType.ShouldEqual(expected.ClientType);
            actual.ConsentType.ShouldEqual(expected.ConsentType);
            actual.DisplayName.ShouldEqual(expected.DisplayName);
            actual.Permissions.Order().ShouldEqual(expected.Permissions.Order());
            actual.Requirements.Order().ShouldEqual(expected.Requirements.Order());
            actual.RedirectUris.OrderBy(_ => _.OriginalString).ShouldEqual(expected.RedirectUris.OrderBy(_ => _.OriginalString));
            actual.PostLogoutRedirectUris.ShouldEqual(expected.PostLogoutRedirectUris);
            actual.DisplayNames.ShouldEqual(expected.DisplayNames);
            actual.Properties.ShouldEqual(expected.Properties);
            actual.Settings.ShouldEqual(expected.Settings);
            actual.JsonWebKeySet.ShouldBeNull();
        }
    }
}
