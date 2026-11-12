// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_signer_selection_would_resample_the_clock : given.a_secret_store_directory
{
    readonly ReloadingClock _clock = new();
    OpenIddictServerOptions _server;
    SigningCredentials? _credential;
    bool _invalidReload;

    void Establish()
    {
        _keys = new SecretStoreKeys(_options, _clock, NullLogger<SecretStoreKeys>.Instance);
        var signer = _keys.Current.Signer;
        _server = new OpenIddictServerOptions();
        _server.SigningCredentials.Add(new SigningCredentials(new X509SecurityKey(signer.Certificate) { KeyId = signer.Kid }, signer.Algorithm));
        File.WriteAllText(Path.Combine(_directory, "signing.a.key"), "invalid test material");
        _clock.OnRead = () =>
        {
            _invalidReload = true;
            _keys.Reload();
        };
    }

    void Because() => _credential = OidcSigningKey.Find(_server, _keys, KeyMaterial.Now);

    [Fact] void should_use_the_supplied_instant_for_one_snapshot() => _invalidReload.ShouldBeFalse();
    [Fact] void should_resolve_the_eligible_credential() => _credential.ShouldNotBeNull();

    class ReloadingClock : TimeProvider
    {
        public Action? OnRead { get; set; }

        public override DateTimeOffset GetUtcNow()
        {
            var onRead = OnRead;
            OnRead = null;
            onRead?.Invoke();
            return KeyMaterial.Now;
        }
    }
}
