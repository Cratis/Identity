# Resource token validation

Cratis ID access tokens are checked in two places: each resource server verifies the JWT locally, and AuthProxy, the edge, introspects at Identity so a revoked token is denied quickly. This page records the decided contract (Cratis/Identity issue 69, decision D20, L0, 2026-11-08) and states what the code shows today.

## The contract

1. **Resource servers do not introspect.** Direct, Studio and Arc-based services validate the JWT signature, issuer and audience locally against the Identity JWKS. They hold no introspection client.
2. **AuthProxy introspects at the edge.** For Cratis ID bearer routes, AuthProxy calls Identity's `connect/introspect`.
   - It keeps a short positive cache, 30 seconds or less, configurable.
   - It never caches a negative result.
   - It fails closed on `/mcp` when introspection is unavailable.
3. **Resources behind the edge rely on AuthProxy.** What reaches a resource depends on AuthProxy's header handling: since AuthProxy 2.50.1 the `Authorization` header is stripped on anonymous routes, and forwarding it is an opt-in (`Service.PassCallerAuthorizationPaths`, AuthProxy issue 283).

Consequence: a revoked token is denied at the edge within the cache window, not at access-token expiry (15 minutes on Identity.Host, at most 5 minutes on hosted-shared).

## Status

| Part | Status | Evidence |
|---|---|---|
| Identity `connect/introspect` endpoint, authenticated callers only | Delivered | `Source/Identity/Clients/ClientDescriptors.cs`; spec `Source/Identity.Integration.Specs/for_Introspection/when_a_client_introspects_an_access_token.cs` |
| Introspection reports an access token inactive after its grant is revoked | Delivered on the train branch `train/identity-20261107-20`, not `main` | Spec `Source/Identity.Integration.Specs/for_Revocation/when_the_cli_revokes_its_refresh_token.cs` (`should_revoke_the_access_token_of_the_grant_for_introspection`) |
| Revocation fence stores that gate token validation | Delivered on the train branch, not `main` | `Source/Identity/Authorization/RejectFencedGrants.cs`, `FencedTokenStore.cs`, `Source/Identity/Revocation/` |
| Local JWT validation at AuthProxy bearer routes | Delivered | AuthProxy `Source/AuthProxy/BearerRoutes/BearerTokenValidator.cs` |
| AuthProxy introspection, positive cache and `/mcp` fail-closed | Decided, pending delivery | AuthProxy issue 255 |
| `Authorization` pass-through opt-in for anonymous routes | Decided, pending delivery | AuthProxy issue 283 |

The shipped `Identity:IntrospectionClients` list is empty in `Source/Identity.Host/appsettings.json` and `Source/Identity.CratisId/appsettings.json`. AuthProxy needs an entry there before it can introspect. No caller of `connect/introspect` exists in Direct, Studio, AuthProxy or Arc today.

## Revocation fence and introspection

Revoking a grant marks its authorization revoked, and the fence stores on the train branch consult that authoritative state before token-entry validation, so introspection of a revoked grant's access token returns `active: false`. The code shows this for a refresh-token revoke followed by introspection of the grant's access token. Identity branches `identity-41-revocation-fence` and `identity-42-revocation-recovery` extend the fence log, outbox and recovery epoch. Whether the outbox or recovery epoch feeds any consumer other than Identity's own token validation is not verified in code; treat it as not part of this contract.

## Failure modes

| Situation | Behavior | Status |
|---|---|---|
| Introspection unavailable at the edge | `/mcp` is denied (fail closed). Other bearer routes follow the AuthProxy route setting. | Decided, pending delivery |
| Token revoked while cached | Still accepted until the positive cache entry expires, at most the configured cache window (30 seconds or less). | Decided, pending delivery |
| Token revoked, then introspected after the cache window | Denied; negative results are never cached. | Decided, pending delivery |
| Unknown membership or tenant | Denied. | Decided; resource check not verified in code |
| Clock skew between edge, resource and Identity | Expiry checks use each host's clock, so skew shifts the effective expiry; keep hosts on synchronized time. Skew does not extend the revocation window, which is cache time. | Not verified in code |
| JWKS rotation | Resources fetch the Identity JWKS; a token signed with a retired key is refused once the key leaves the set. Identity publishes keys through JWKS (`Source/Identity/Keys/`). | Resource-side refresh behavior not verified in code |
| Resource reached without the edge | Only local JWT checks apply, so a revoked token stays accepted until it expires (15 minutes on Identity.Host, at most 5 minutes on hosted-shared). Route resources through AuthProxy. | Follows from the contract |

## Where this appears in the readiness checklist

Steps 8 and 12 of the [end-to-end checklist](../e2e/2479-checklist.md) depend on this contract: step 8 is the edge introspection caller, and step 12 is the revoke-then-call denial.
