// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_an_invalid_reload_follows_a_readiness_check : given.a_secret_store_directory
{
    OpenIddictServerOptions _server;
    bool _previouslyReady;
    SigningCredentials? _credential;

    void Establish()
    {
        Open();
        var signer = _keys.Current.Signer;
        _server = new OpenIddictServerOptions();
        _server.SigningCredentials.Add(new SigningCredentials(new X509SecurityKey(signer.Certificate) { KeyId = signer.Kid }, signer.Algorithm));
    }

    void Because()
    {
        _previouslyReady = _keys.CanIssueIdentityTokens;
        File.WriteAllText(Path.Combine(_directory, "signing.a.key"), "invalid test material");
        _keys.Reload();
        _credential = OidcSigningKey.Find(_server, _keys, _time.GetUtcNow());
    }

    [Fact] void should_start_ready() => _previouslyReady.ShouldBeTrue();
    [Fact] void should_not_admit_a_credential_using_stale_readiness() => _credential.ShouldBeNull();
    [Fact] void should_retain_the_last_snapshot_for_oauth() => _keys.CanIssue.ShouldBeTrue();
}
