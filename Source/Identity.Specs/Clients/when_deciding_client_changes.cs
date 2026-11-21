// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.ChangingRedirectUris;
using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Clients.Retiring;
using Cratis.Identity.Clients.RotatingSecret;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients;

public class when_deciding_client_changes : given.client_commands
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_mutations_of_missing_or_retired_clients(bool retired)
    {
        var state = retired ? _active with { Retired = true } : new ClientRegistration(_id.Value);
        new ChangeRedirectUris(_id, []).Handle(state).TryGetError(out _).ShouldBeTrue();
        new RotateSecret(_id, Plaintext, new(Now, Now)).Handle(state, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
        new RetireClient(_id).Handle(state, TimeProvider.System).TryGetError(out _).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_reversed_rotation_deadlines_and_backwards_activation()
    {
        new RotateSecret(_id, Plaintext, new(Now, Now.AddSeconds(-1))).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
        new RotateSecret(_id, Plaintext, new(_active.ActivatedAt.AddSeconds(-1), Now)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
        new RotateSecret(_id, Plaintext, new(Now, Now)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_public_secret_rotation_and_missing_confidential_secrets()
    {
        var state = _active with { Configuration = _configuration with { ClientType = ClientType.Public } };
        new RotateSecret(_id, Plaintext, new(Now, Now)).Handle(state, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
        new RegisterClient(new("spec-client"), _configuration).Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldBeTrue();
        new RotateSecret(_id, " ", new(Now, Now)).Handle(_active, _digester, _clock, _secretOptions).TryGetError(out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,hostile")]
    [InlineData("http://client.example/callback")]
    [InlineData("http://127.0.0.1/callback")]
    [InlineData("https://client.example/callback#fragment")]
    [InlineData("https://user:password@client.example/callback")]
    void should_refuse_unsafe_web_redirects_on_registration_and_changes(string redirect)
    {
        var configuration = _configuration with { RedirectUris = [redirect] };
        new RegisterClient(new("spec-client"), configuration, Plaintext).Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldBeTrue();
        new ChangeRedirectUris(_id, [redirect]).Handle(_active).TryGetError(out _).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_null_redirects_as_validation_errors()
    {
        new RegisterClient(new("spec-client"), _configuration with { RedirectUris = null! }, Plaintext).Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldBeTrue();
        new ChangeRedirectUris(_id, null!).Handle(_active).TryGetError(out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://localhost/callback")]
    [InlineData("http://192.0.2.1/callback")]
    [InlineData("https://127.0.0.1/callback")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://[::1]/callback#fragment")]
    void should_refuse_non_literal_or_non_http_native_redirects(string redirect)
    {
        var native = new ClientConfiguration { ApplicationType = ClientApplicationType.Native, RedirectUris = [redirect] };
        new RegisterClient(new("native-spec"), native).Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldBeTrue();
        new ChangeRedirectUris(_id, [redirect]).Handle(_active with { Configuration = native, SecretHash = null }).TryGetError(out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://127.0.0.1/callback")]
    [InlineData("http://[::1]:49152/callback")]
    void should_preserve_native_literal_loopback_and_pkce_rules(string redirect)
    {
        var native = new ClientConfiguration { ApplicationType = ClientApplicationType.Native, RedirectUris = [redirect] };
        new RegisterClient(new("native-spec"), native).Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldBeFalse();
        new RegisterClient(new("native-spec"), native with { RequirePkce = false }).Handle(_descriptors, _clock, _digester).TryGetError(out _).ShouldBeTrue();
    }

    [Fact]
    void should_return_only_the_requested_redirect_or_retirement_fact()
    {
        var changed = new ChangeRedirectUris(_id, ["https://client.example/new"]).Handle(_active).AsT0.Single();
        ((ClientRedirectUrisChanged)changed.Event).RedirectUris.ShouldEqual(["https://client.example/new"]);
        changed.EventSourceId.Value.ShouldEqual(_id.Value.ToString());
        var retired = new RetireClient(_id).Handle(_active, TimeProvider.System).AsT0.Single();
        ((ClientRetired)retired.Event).ReasonId.ShouldEqual("owner_retired");
    }
}
