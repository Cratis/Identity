// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Contracts.Compliance;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;

namespace Cratis.Identity.for_ClientAndResourceEvents;

public class when_inspecting_the_contracts : Specification
{
    [Theory]
    [InlineData(typeof(ClientRegistered), "0e4bb9ea-86c7-4000-bbfd-308a2dbf4989", "Internal", "ApplicationType,ClientId,ClientType,ConsentType,DisplayName,Endpoints,GrantTypes,RegisteredAt,RegistrationKind,RequirePkce,ResourceIndicators,ResponseTypes,Scopes")]
    [InlineData(typeof(ClientRedirectUrisChanged), "1dfa8ff9-2538-44c2-8628-00dc084fcabe", "Internal", "RedirectUris")]
    [InlineData(typeof(ClientSecretRotated), "4a913d36-056b-4e71-81ca-c90b207393b3", "Internal", "ActivatedAt,ClientSecretHash,ClientSecretId,PreviousValidUntil")]
    [InlineData(typeof(ClientRetired), "8fa4f300-a66f-442d-ad15-37dc3bdd692f", "Internal", "ReasonId,RetiredAt")]
    [InlineData(typeof(ResourceRegistered), "e57de89b-215d-43ab-a235-83a5e517934a", "Public", "Audience,ResourceIndicator,ScopePrefix")]
    [InlineData(typeof(ResourceScopeRegistered), "24b65e88-8dbb-4e02-8f37-9d591eb29d20", "Public", "ScopeName")]
    [InlineData(typeof(ResourceScopeRetired), "6a6869c9-423b-4746-8aa5-93b989b6895f", "Public", "RetiredAt,ScopeName")]
    public void should_pin_the_generation_one_contract(Type type, string id, string visibility, string fields)
    {
        var shape = ReflectionShapeBuilder.Build(type);
        shape.TypeId.ShouldEqual(id);
        shape.Generation.ShouldEqual(1u);
        shape.Visibility.ShouldEqual(visibility);
        string.Join(',', shape.Members.Select(member => member.ClrName).Order(StringComparer.Ordinal)).ShouldEqual(fields);
        ContentRules.Check(shape, SafeConceptRegistry.Entries).ShouldBeEmpty();
    }

    [Fact]
    void should_use_only_the_current_endpoint_permissions() => Enum.GetNames<ClientEndpoint>().ShouldEqual(["Authorization", "Token", "Revocation", "Introspection", "DeviceAuthorization"]);

    [Fact]
    void should_use_only_the_current_grant_permissions() => Enum.GetNames<ClientGrantType>().ShouldEqual(["AuthorizationCode", "RefreshToken", "DeviceCode", "TokenExchange"]);

    [Fact]
    void should_use_only_the_current_response_permissions() => Enum.GetNames<ClientResponseType>().ShouldEqual(["Code"]);

    [Fact]
    void should_keep_the_amendment_consent_choices() => Enum.GetNames<ClientConsentType>().ShouldEqual(["None", "Explicit"]);

    [Fact]
    void should_allocate_independent_uuid_streams_and_secret_references()
    {
        Guid[] ids = [ClientRegistrationId.New().Value, ClientRegistrationId.New().Value, ResourceId.New().Value, ResourceId.New().Value, ClientSecretId.New().Value, ClientSecretId.New().Value];
        ids.Distinct().Count().ShouldEqual(ids.Length);
        ids.ShouldNotContain(Guid.Empty);
        ids.All(id => id.ToString("D")[14] == '4').ShouldBeTrue();
    }
}
