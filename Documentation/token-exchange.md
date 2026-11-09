# Token exchange

A product behind an identity provider Identity trusts, such as AuthProxy fronting a partner login, often holds a signed token from that provider and needs a Cratis access token for a person in one tenant. Token exchange (RFC 8693) is the grant for that: the trusted client presents the provider's token and receives a short-lived Identity access token for exactly one resource and one tenant. It issues nothing unless every check passes, and the grant is disabled until you configure it.

## Request

The client authenticates with its client secret and posts to the token endpoint:

```http
POST /connect/token
Authorization: Basic <base64(client_id:client_secret)>
Content-Type: application/x-www-form-urlencoded

grant_type=urn:ietf:params:oauth:grant-type:token-exchange
&subject_token=<external-jwt>
&subject_token_type=urn:ietf:params:oauth:token-type:jwt
&resource=https://cratis.direct/mcp
&scope=direct:mcp:read
&tenant=<tenant-id>
```

| Parameter | Rule |
| --- | --- |
| `subject_token` | Required, once. A signed JWT from a configured issuer. |
| `subject_token_type` | Required. `urn:ietf:params:oauth:token-type:jwt` or `urn:ietf:params:oauth:token-type:access_token`. |
| `resource` | Required, once. A resource indicator the client is allowed to use. |
| `scope` | Required. Every scope must belong to the resource and be allowed for the client. `offline_access` is refused. |
| `tenant` | Required, once. Must be a tenant the product lists for this person on this resource. |
| `requested_token_type` | Optional. Only `urn:ietf:params:oauth:token-type:access_token`. |
| `actor_token`, `audience` | Refused. There is no delegation. |

The response is a standard token response with `token_type` `Bearer`, `issued_token_type` `urn:ietf:params:oauth:token-type:access_token`, `expires_in` and `access_token`. No refresh token is ever issued; exchange again when it expires.

```json
{
  "access_token": "<jwt>",
  "token_type": "Bearer",
  "issued_token_type": "urn:ietf:params:oauth:token-type:access_token",
  "expires_in": 300
}
```

## Configuration

Nothing is enabled by default. The section is `Identity:TokenExchange`; a client without an entry cannot use the grant.

| Setting | Default | Meaning |
| --- | --- | --- |
| `Issuers[].Issuer` | — | Exact `iss` value, an absolute HTTPS URI without a fragment. |
| `Issuers[].JwksUri` | — | Where signing keys are fetched. Keys are never taken from the token. HTTPS required outside Development. |
| `Issuers[].Audiences[]` | — | Accepted `aud` values. At least one. |
| `Issuers[].SigningAlgorithms[]` | `RS256`, `ES256` | Asymmetric algorithms only; `none` and HMAC are refused. |
| `Issuers[].ClockSkew` | `00:01:00` | Tolerance on `exp` and `iat`. |
| `Issuers[].MaxTokenAge` | `00:10:00` | A token older than this (from `iat`) is refused. At most one hour. |
| `Issuers[].JwksTimeout` | `00:00:03` | Per-fetch timeout. At most ten seconds. |
| `Clients[].ClientId`, `ClientSecret` | — | The confidential client. The secret needs at least 32 characters. |
| `Clients[].Issuers[]` | — | Issuers this client may present tokens from. |
| `Clients[].Resources[]` | — | Resource audiences this client may request. |
| `Clients[].Scopes[]` | — | Scope ceiling; empty means any scope the resource owns. |

The access token lifetime is `Tokens:AccessTokenLifetime`; use five minutes or less for exchange clients.

## Errors

Every failure is an RFC error with a fixed description. Failures about the person or the subject token are deliberately indistinguishable.

| Error | When |
| --- | --- |
| `invalid_client` | Missing or wrong client secret. |
| `unauthorized_client` | The client is not enabled for this grant. |
| `invalid_request` | A parameter is missing, repeated or unsupported. |
| `invalid_target` | Unknown resource, or not allowed for this client. |
| `invalid_scope` | A scope the resource does not own, or outside the client's ceiling. |
| `invalid_grant` | Anything else about the subject token or person: unknown issuer, bad signature, expired or too old, replayed, not linked to an account, account inactive, not a member of the tenant. One identical description for all. |
| `temporarily_unavailable` | The issuer keys, product tenants callback or replay store could not be reached, or the replay store is at capacity. Nothing is issued; retry. |

## What the token says

The access token follows the [access-token profile](specs/identity-access-token-v1.md#token-exchange-issuance-profile): `aud` is the resource's audience, `tid` the requested tenant, `sub` the Identity account, `azp` and `client_id` the exchange client, and `idp` the external issuer.

## Security notes

- **Linking.** A person is found only through the linked `(issuer, subject)` pair of an active account. Email addresses and usernames are never matched.
- **Replay.** A subject token is accepted once. The guard uses the shared operational MongoDB store with majority durability and primary reads, so replay protection survives restarts and spans replicas. Each verified replay key is stored atomically until `min(exp, iat + MaxTokenAge) + ClockSkew`; TTL cleanup is asynchronous, and expired leftovers are replaced atomically. A soft limit of 1,000,000 entries (including expired entries awaiting cleanup) and a five-second store deadline fail closed with `temporarily_unavailable`; no token is issued when recording fails. Keep subject tokens short-lived and `MaxTokenAge` small.
- **`temporarily_unavailable` is intentional.** During an issuer-key (JWKS), tenants-callback or replay-store outage the grant answers `temporarily_unavailable` rather than `invalid_grant`, so clients retry correctly instead of treating the person as refused. It leaks only that the issuer is configured, or that the token or account was valid, during an outage; the exchange is opt-in, so that exposure is limited to deployments that enabled it.
- **Short life, no revocation grant.** The issued token cannot be revoked individually and has no refresh token. Its short lifetime is the bound. Membership is checked live when the token is issued; products must check it again on each request.
- **Bearer, not DPoP.** There is no actor, so the delegation rule in [DPoP](specs/identity-dpop-v1.md) does not apply; the consumer validates the token like any Identity access token.
- **No token-endpoint rate limit yet.** The grant is not rate limited beyond the issuer key refresh, which is limited to once a minute per issuer. Put the token endpoint behind your usual edge limits.
- **Tenants callback.** Membership comes from the product tenants callback, which receives the GitHub identity of the account as it does for sign-in. A person who signed in through a non-GitHub issuer has the same upstream link and is looked up the same way; the callback body does not yet name the external issuer.
- **Logs.** Refusals log the client, the configured issuer and a category. Replay-store outages use `ReplayGuardUnavailable` with detail `replay-guard-unavailable`; capacity refusals use `ReplayGuardFull` with detail `replay-guard-full`. Tokens and claim values are never logged.
