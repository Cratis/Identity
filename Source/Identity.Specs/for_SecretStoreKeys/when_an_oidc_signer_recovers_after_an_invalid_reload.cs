// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_an_oidc_signer_recovers_after_an_invalid_reload : given.a_secret_store_directory
{
    OpenIddictServerOptions _server;
    bool _initiallyUsable;
    bool _oidcRefused;
    bool _oauthStillUsable;
    bool _recovered;

    void Establish()
    {
        Open();
        var signer = _keys.Current.Signer;
        _server = new OpenIddictServerOptions();
        _server.SigningCredentials.Add(new SigningCredentials(new X509SecurityKey(signer.Certificate) { KeyId = signer.Kid }, signer.Algorithm));
        _initiallyUsable = OidcSigningKey.Find(_server, _keys, _time.GetUtcNow()) is not null;
    }

    void Because()
    {
        File.WriteAllText(Path.Combine(_directory, "signing.a.key"), "invalid test material");
        _keys.Reload();
        _oidcRefused = OidcSigningKey.Find(_server, _keys, _time.GetUtcNow()) is null;
        _oauthStillUsable = _keys.CanIssue;
        _files.WriteTo(_directory);
        _keys.Reload();
        _recovered = OidcSigningKey.Find(_server, _keys, _time.GetUtcNow()) is not null;
    }

    [Fact] void should_start_with_a_usable_signer() => _initiallyUsable.ShouldBeTrue();
    [Fact] void should_refuse_oidc_after_invalid_reload() => _oidcRefused.ShouldBeTrue();
    [Fact] void should_preserve_the_existing_oauth_reload_behavior() => _oauthStillUsable.ShouldBeTrue();
    [Fact] void should_restore_oidc_without_restart() => _recovered.ShouldBeTrue();
}
