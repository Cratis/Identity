// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Cratis.Identity.for_ClientIdMetadataDocuments;

public class when_advertising_and_validating_options : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_only_advertise_the_enabled_public_profile(bool enabled)
    {
        var context = new OpenIddictServerEvents.HandleConfigurationRequestContext(new OpenIddictServerTransaction
        {
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions(),
            Request = new OpenIddictRequest(),
            Response = new OpenIddictResponse()
        });
        await new AdvertiseClientIdMetadataDocuments(new ClientIdMetadataOptions { Enabled = enabled }).HandleAsync(context);
        context.Metadata.ContainsKey("client_id_metadata_document_supported").ShouldEqual(enabled);
        context.TokenEndpointAuthenticationMethods.Contains("none").ShouldEqual(enabled);
        context.Metadata.ContainsKey("registration_endpoint").ShouldBeFalse();
        context.Metadata.ContainsKey("client_registration_types_supported").ShouldBeFalse();
    }

    [Fact]
    async Task should_be_disabled_by_default_and_never_fetch_an_invalid_url()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var resolver = new ClientIdMetadataDocuments(fetcher, new(), Substitute.For<IResourceCatalog>(), TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance);
        (await resolver.Resolve(new ClientId("https://client.example/client.json"), CancellationToken.None)).Descriptor.ShouldBeNull();
        await fetcher.DidNotReceiveWithAnyArgs().FetchAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    async Task should_enforce_exact_host_allowlists_before_fetching()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var resolver = new ClientIdMetadataDocuments(fetcher, new() { Enabled = true, AllowedHosts = ["approved.example"] }, Substitute.For<IResourceCatalog>(), TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance);
        (await resolver.Resolve(new ClientId("https://unapproved.example/client.json"), CancellationToken.None)).Descriptor.ShouldBeNull();
        await fetcher.DidNotReceiveWithAnyArgs().FetchAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact] void should_validate_default_limits() => IdentityOptionsClientsValidation.Validate(new()).ShouldBeEmpty();

    [Fact]
    void should_refuse_invalid_resource_limits_even_when_disabled()
    {
        var options = new ClientIdMetadataOptions
        {
            MaxDocumentBytes = 65537,
            RequestTimeout = TimeSpan.FromSeconds(10),
            ConnectTimeout = TimeSpan.Zero,
            MinCacheTtl = TimeSpan.Zero,
            MaxCacheTtl = TimeSpan.FromHours(25),
            MaxCacheEntries = 0,
            MaxConcurrentFetches = 0,
            MaxRedirectUris = 11,
            MaxFetchesPerMinute = 0,
            MaxFetchesPerHostPerMinute = 0,
            AllowedHosts = ["localhost"]
        };
        IdentityOptionsClientsValidation.Validate(options).Count().ShouldEqual(11);
    }
}
