// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenIddict.Server;

namespace Cratis.Identity.Keys;

/// <summary>
/// Rebuilds the <see cref="OpenIddictServerOptions"/> when the keys of the secret store change.
/// </summary>
/// <param name="keys">The <see cref="SecretStoreKeys"/>.</param>
internal class SecretStoreKeysChangeTokenSource(SecretStoreKeys keys) : IOptionsChangeTokenSource<OpenIddictServerOptions>
{
    /// <inheritdoc/>
    public string Name => Options.DefaultName;

    /// <inheritdoc/>
    public IChangeToken GetChangeToken() => keys.ChangeToken;
}
