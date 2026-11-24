// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
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
    [InlineData(typeof(ClientRegistered), "ClientRegistered", "Internal", "ApplicationType,ClientId,ClientType,ConsentType,DisplayName,Endpoints,GrantTypes,RegisteredAt,RegistrationKind,RequirePkce,ResourceIndicators,ResponseTypes,Scopes")]
    [InlineData(typeof(ClientRedirectUrisChanged), "ClientRedirectUrisChanged", "Internal", "RedirectUris")]
    [InlineData(typeof(ClientSecretRotated), "ClientSecretRotated", "Internal", "ActivatedAt,ClientSecretHash,ClientSecretId,PreviousValidUntil")]
    [InlineData(typeof(ClientRetired), "ClientRetired", "Internal", "ReasonId,RetiredAt")]
    [InlineData(typeof(ClientAdded), "ClientAdded", "Public", "ApplicationType,ClientId,ClientType,ConsentType,Endpoints,GrantTypes,RegisteredAt,RegistrationKind,RequirePkce,ResourceIndicators,ResponseTypes,Scopes")]
    [InlineData(typeof(RedirectUrisChangedForClient), "RedirectUrisChangedForClient", "Public", "RedirectUris")]
    [InlineData(typeof(SecretRotatedForClient), "SecretRotatedForClient", "Public", "ActivatedAt,ClientSecretId,PreviousValidUntil")]
    [InlineData(typeof(ClientPermanentlyRetired), "ClientPermanentlyRetired", "Public", "ReasonId,RetiredAt")]
    [InlineData(typeof(ResourceRegistered), "ResourceRegistered", "Public", "Audience,ResourceIndicator,ScopePrefix")]
    [InlineData(typeof(ResourceScopeRegistered), "ResourceScopeRegistered", "Public", "ScopeName")]
    [InlineData(typeof(ResourceScopeRetired), "ResourceScopeRetired", "Public", "RetiredAt,ScopeName")]
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

    [Theory]
    [InlineData(typeof(ClientEndpoint), "Authorization=0,Token=1,Revocation=2,Introspection=3,DeviceAuthorization=4")]
    [InlineData(typeof(ClientGrantType), "AuthorizationCode=0,RefreshToken=1,DeviceCode=2,TokenExchange=3")]
    [InlineData(typeof(ClientResponseType), "Code=0")]
    [InlineData(typeof(ClientConsentType), "None=0,Explicit=1")]
    [InlineData(typeof(ClientType), "Public=0,Confidential=1")]
    [InlineData(typeof(ClientApplicationType), "Native=0,Web=1")]
    [InlineData(typeof(ClientRegistrationKind), "PreRegistered=0,ClientIdentifierMetadataDocument=1,DynamicClientRegistration=2")]
    public void should_pin_each_enum_name_to_its_numeric_assignment(Type type, string expected) =>
        string.Join(',', Enum.GetNames(type).Select(name => $"{name}={Convert.ToInt32(Enum.Parse(type, name), CultureInfo.InvariantCulture)}")).ShouldEqual(expected);

    [Fact]
    void should_allocate_independent_uuid_streams_and_secret_references()
    {
        Guid[] ids = [ClientRegistrationId.New().Value, ClientRegistrationId.New().Value, ResourceId.New().Value, ResourceId.New().Value, ClientSecretId.New().Value, ClientSecretId.New().Value];
        ids.Distinct().Count().ShouldEqual(ids.Length);
        ids.ShouldNotContain(Guid.Empty);
        ids.All(id => id.ToString("D")[14] == '4').ShouldBeTrue();
    }
}
