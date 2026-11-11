# Direct MCP resources and audiences

```text
Doc id: direct-mcp-resources-and-audiences
Version: 1
Status: Draft
```

## Decision

Direct accepts Identity tokens for exactly two resources: `direct-mcp` (`https://app.cratis.direct/mcp`) and `direct-v1` (`https://app.cratis.direct/v1`). Both use explicit scopes only; a literal `*` is never a scope.

Resource metadata, resource validation and Direct's configured public origin must match exactly. There is no `direct-api` resource, and tokens issued for another product, such as Studio, are refused.

Direct binds the validated `tid` claim to its canonical tenant and checks the caller's current membership on every call. Equal `tid` values grant nothing by themselves.

## Context

Identity previously registered the Direct indicators on the bare `cratis.direct` host. Direct is served from `app.cratis.direct`, and a protected resource must advertise the origin clients actually reach. A resource that accepts any audience, or that trusts `tid` alone, would let a token minted for another product, or for a user who has left the tenant, reach Direct.

## Consequences

- The shipped Cratis ID configuration uses the `app.cratis.direct` indicators.
- `direct-mcp` and `direct-v1` stay separate audiences with separate scope sets; a token for one is refused by the other.
- Direct owns the per-call membership check; Identity does not replace it.

## Required specs and acceptance

- The shipped Cratis ID resources `direct-mcp` and `direct-v1` use the `app.cratis.direct` indicators and explicit scopes without `*`.
- No resource named `direct-api` exists.
- A request for a Studio or any other unregistered indicator never yields a Direct audience.
- Direct (in its own repository) rejects: `direct-api`, a Studio audience, a wildcard scope, an origin mismatch, and a non-member with a valid `tid`.
