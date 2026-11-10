// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients.given;

public class client_commands : Specification
{
    protected static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddDays(10);
    protected const string Plaintext = "synthetic-client-secret-for-specs-only";
    protected ClientRegistrationId _id;
    private protected ClientSecretDigester _digester;
    private protected ClientConfiguration _configuration;
    private protected ClientRegistration _active;

    void Establish()
    {
        _id = ClientRegistrationId.New();
        _digester = new(Options.Create(new ClientSecretOptions { Key = Convert.ToBase64String(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()) }));
        _configuration = new()
        {
            ClientType = ClientType.Confidential,
            RedirectUris = ["https://client.example/callback"],
            Endpoints = [ClientEndpoint.Authorization, ClientEndpoint.Token],
            GrantTypes = [ClientGrantType.AuthorizationCode],
            ResponseTypes = [ClientResponseType.Code],
            Scopes = ["openid"]
        };
        _active = new(_id.Value)
        {
            ClientId = new("spec-client"),
            Configuration = _configuration,
            SecretId = ClientSecretId.New(),
            SecretHash = _digester.Digest("synthetic-previous-secret"),
            ActivatedAt = Now.AddDays(-1)
        };
    }
}
