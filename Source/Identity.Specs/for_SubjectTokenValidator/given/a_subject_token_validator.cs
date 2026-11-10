// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_SubjectTokenValidator.given;

public class a_subject_token_validator : Specification
{
    protected IdentityOptions options;
    protected FixedTimeProvider time;
    private protected IExternalIssuerKeys keys;
    private protected SubjectTokenReplayGuard replay;
    private protected SubjectTokenValidator validator;
    protected ExternalIssuerFixture issuer;

    protected ExternalIssuerOptions IssuerOptions => options.TokenExchange.Issuers[0];
    protected TokenExchangeClientOptions Client => options.TokenExchange.Clients[0];

    void Establish()
    {
        options = Configurations.WithTokenExchange();
        time = new(DateTimeOffset.UtcNow);
        issuer = new();
        keys = Substitute.For<IExternalIssuerKeys>();
        keys.Get(default!, default, default).ReturnsForAnyArgs(Task.FromResult<IReadOnlyCollection<SecurityKey>?>(issuer.PublicKeys));
        replay = new(time);
        validator = new(options.AsOptions(), keys, time);
    }

    protected Dictionary<string, object> Payload() => ExternalIssuerFixture.Payload(time.Now);

    protected string Valid() => ExternalIssuerFixture.Sign(Payload(), issuer.RsaCredentials);

    private protected Task<SubjectTokenResult> Validate(string token) => validator.Validate(token, Client);

    private protected async Task<SubjectTokenRefusal> RefusalOf(string token) => (await Validate(token)).Refusal;

    void Destroy() => issuer.Dispose();
}
