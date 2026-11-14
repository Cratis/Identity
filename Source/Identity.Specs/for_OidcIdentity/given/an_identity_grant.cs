// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.for_OidcIdentity.given;

public class an_identity_grant : Specification
{
    protected const string ClientId = "oidc-web";
    protected const string SessionId = "example-site-session";
    protected static readonly DateTimeOffset AuthTime = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
    private protected Account _account;

    void Establish() => _account = Identity.given.GitHubAccounts.Create(new("7d9d5c9e-0000-4000-8000-000000000001"), "Example Member", null, "123456", "example-member");
}
