// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Identity.Clients.MetadataDocuments;
using static OpenIddict.Abstractions.OpenIddictConstants;
using IdentityOptionsClients = Cratis.Identity.Clients.MetadataDocuments.ClientIdMetadataOptions;

namespace Cratis.Identity.for_MetadataDocumentParser;

public class when_validating_metadata : Specification
{
    const string ClientId = "https://client.example/cimd.json";
    const string Valid = "\"client_id\":\"https://client.example/cimd.json\",\"redirect_uris\":[\"https://client.example/callback\"]";

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData(/*lang=json,strict*/ "{\"redirect_uris\":[\"https://client.example/callback\"]}")]
    [InlineData(/*lang=json,strict*/ "{\"client_id\":\"https://other.example/cimd.json\",\"redirect_uris\":[\"https://client.example/callback\"]}")]
    [InlineData(/*lang=json,strict*/ "{\"client_id\":\"HTTPS://client.example/cimd.json\",\"redirect_uris\":[\"https://client.example/callback\"]}")]
    [InlineData(/*lang=json,strict*/ "{\"client_id\":\"https://client.example/cimd.json/\",\"redirect_uris\":[\"https://client.example/callback\"]}")]
    [InlineData(/*lang=json,strict*/ "{\"client_id\":\"https://client.example:443/cimd.json\",\"redirect_uris\":[\"https://client.example/callback\"]}")]
    public void should_refuse_invalid_roots_and_identity(string json) => Parse(json).Descriptor.ShouldBeNull();

    [Theory]
    [InlineData("client_secret", "\"synthetic\"")]
    [InlineData("jwks", "{}")]
    [InlineData("jwks_uri", "\"https://client.example/keys\"")]
    [InlineData("request_uris", "[]")]
    [InlineData("software_statement", "\"synthetic\"")]
    [InlineData("token_endpoint_auth_method", "\"client_secret_basic\"")]
    [InlineData("token_endpoint_auth_methods_supported", "[\"private_key_jwt\"]")]
    [InlineData("token_endpoint_auth_methods_supported", "[\"none\",\"client_secret_basic\"]")]
    [InlineData("token_endpoint_auth_methods_supported", "\"none\"")]
    [InlineData("token_endpoint_auth_methods_supported", "[]")]
    [InlineData("grant_types", "[\"client_credentials\"]")]
    [InlineData("grant_types", "[\"implicit\"]")]
    [InlineData("grant_types", "[\"urn:ietf:params:oauth:grant-type:token-exchange\"]")]
    [InlineData("response_types", "[\"token\"]")]
    [InlineData("response_types", "[]")]
    [InlineData("client_name", "42")]
    [InlineData("request_object_signing_alg", "\"RS256\"")]
    public void should_refuse_unsupported_privileges_and_presentations(string field, string value) => Parse("{" + Valid + ",\"" + field + "\":" + value + "}").Descriptor.ShouldBeNull();

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,synthetic")]
    [InlineData("file:///callback")]
    [InlineData("myapp://callback")]
    [InlineData("https://127.0.0.1/callback")]
    [InlineData("https://localhost/callback")]
    [InlineData("https://user@client.example/callback")]
    [InlineData("https://client.example/callback#part")]
    [InlineData("http://localhost/callback")]
    [InlineData("http://127.0.0.2/callback")]
    [InlineData("http://127.0.0.1:49152/callback")]
    public void should_refuse_unsafe_redirects(string redirect) => Parse("{\"client_id\":\"" + ClientId + "\",\"redirect_uris\":[\"" + redirect + "\"]}").Descriptor.ShouldBeNull();

    [Theory]
    [InlineData("\\ud800")]
    [InlineData("\\udc00")]
    [InlineData("\\ud800x")]
    public void should_refuse_invalid_unicode_identity(string value) => Parse("{\"client_id\":\"" + value + "\",\"redirect_uris\":[]}").Refusal.ShouldEqual(MetadataRefusal.Json);

    [Theory]
    [InlineData("\"client_name\":\"\\ud800\"")]
    [InlineData("\"client_name\":\"\\udc00\"")]
    [InlineData("\"client_name\":\"\\ud800x\"")]
    [InlineData("\"\\ud800\":true")]
    [InlineData("\"\\udc00\":true")]
    [InlineData("\"\\ud800x\":true")]
    public void should_refuse_invalid_unicode_members(string member) => Parse("{" + Valid + "," + member + "}").Refusal.ShouldEqual(MetadataRefusal.Json);

    [Theory]
    [InlineData("\\ud800")]
    [InlineData("\\udc00")]
    [InlineData("\\ud800x")]
    public void should_refuse_invalid_unicode_redirects(string value) => Parse("{\"client_id\":\"" + ClientId + "\",\"redirect_uris\":[\"https://client.example/" + value + "\"]}").Refusal.ShouldEqual(MetadataRefusal.Json);

    [Theory]
    [InlineData(new byte[] { 0xff })]
    [InlineData(new byte[] { 0xc3, 0x28 })]
    [InlineData(new byte[] { 0xed, 0xa0, 0x80 })]
    public void should_refuse_malformed_utf8(byte[] bytes) => new MetadataDocumentParser(new IdentityOptionsClients()).Parse(ClientId, bytes).Refusal.ShouldEqual(MetadataRefusal.Json);

    [Fact] void should_refuse_duplicate_keys() => Parse("{" + Valid + ",\"client_id\":\"" + ClientId + "\"}").Descriptor.ShouldBeNull();
    [Fact] void should_refuse_duplicate_nested_keys() => Parse("{" + Valid + ",\"extension\":{\"x\":1,\"x\":2}}").Descriptor.ShouldBeNull();
    [Fact] void should_refuse_deep_json() => Parse("{" + Valid + ",\"x\":" + new string('[', 9) + "0" + new string(']', 9) + "}").Descriptor.ShouldBeNull();
    [Fact] void should_refuse_duplicate_redirects() => Parse("{\"client_id\":\"" + ClientId + "\",\"redirect_uris\":[\"https://client.example/cb\",\"https://client.example/cb\"]}").Descriptor.ShouldBeNull();
    [Fact] void should_refuse_mixed_native_and_web_redirects() => Parse("{\"client_id\":\"" + ClientId + "\",\"redirect_uris\":[\"https://client.example/cb\",\"http://127.0.0.1/cb\"]}").Descriptor.ShouldBeNull();
    [Fact] void should_refuse_an_empty_redirect_set() => Parse("{\"client_id\":\"" + ClientId + "\",\"redirect_uris\":[]}").Descriptor.ShouldBeNull();
    [Fact] void should_bound_an_overlong_name() => Parse("{" + Valid + ",\"client_name\":\"" + new string('a', 201) + "\"}").Descriptor.DisplayName.Length.ShouldEqual(100);
    [Fact] void should_strip_controls_and_directional_formatting() => Parse("{" + Valid + ",\"client_name\":\"  Client\\n\\u202e  Name  \"}").Descriptor.DisplayName.ShouldEqual("Client Name");
    [Fact] void should_ignore_document_scope_and_presentation_urls() => Parse("{" + Valid + ",\"scope\":\"openid root\",\"logo_uri\":\"http://169.254.169.254/\"}").Descriptor.Permissions.ShouldNotContain(Permissions.Prefixes.Scope + "root");
    [Fact] void should_accept_the_draft_plural_authentication_spelling() => Parse("{" + Valid + ",\"token_endpoint_auth_methods_supported\":[\"none\"]}").Descriptor.ShouldNotBeNull();
    [Fact] void should_not_grant_refresh_when_it_is_not_listed() => Parse("{" + Valid + "}").Descriptor.Permissions.ShouldNotContain(Permissions.GrantTypes.RefreshToken);
    [Fact] void should_refuse_unsupported_authentication_with_a_bounded_reason() => Parse("{" + Valid + ",\"token_endpoint_auth_method\":\"private_key_jwt\"}").Refusal.ShouldEqual(MetadataRefusal.UnsupportedAuthMethod);
    [Fact] void should_ignore_unrelated_extensions() => Parse("{" + Valid + ",\"extension\":true}").Descriptor.ShouldNotBeNull();

    [Fact]
    void should_force_the_web_security_shape()
    {
        var descriptor = Parse("{" + Valid + "}").Descriptor;
        descriptor.ApplicationType.ShouldEqual(ApplicationTypes.Web);
        descriptor.ClientType.ShouldEqual(ClientTypes.Public);
        descriptor.ConsentType.ShouldEqual(ConsentTypes.Explicit);
        descriptor.Requirements.ShouldContain(Requirements.Features.ProofKeyForCodeExchange);
        descriptor.Permissions.ShouldNotContain(Permissions.Endpoints.Introspection);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public void should_classify_loopback_literals_as_native(string host) => Parse("{\"client_id\":\"" + ClientId + "\",\"redirect_uris\":[\"http://" + host + "/callback\"]}").Descriptor.ApplicationType.ShouldEqual(ApplicationTypes.Native);

    [Fact]
    void should_only_allow_localhost_with_explicit_policy()
    {
        var parser = new MetadataDocumentParser(new IdentityOptionsClients { AllowLocalhostLoopback = true });
        parser.Parse(ClientId, Encoding.UTF8.GetBytes("{\"client_id\":\"" + ClientId + "\",\"redirect_uris\":[\"http://localhost/callback\"]}")).Descriptor.ApplicationType.ShouldEqual(ApplicationTypes.Native);
    }

    static MetadataDocumentResult Parse(string json) => new MetadataDocumentParser(new IdentityOptionsClients()).Parse(ClientId, Encoding.UTF8.GetBytes(json));
}
