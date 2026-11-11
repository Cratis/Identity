# Edge trust on Direct bearer routes

```text
Doc id: direct-bearer-route-edge-trust
Version: 1
Status: Draft
```

## Decision

On bearer routes the AuthProxy validates issuer, audience, scope and `tid` only. The `IgnoreDeploymentRequiredClaims` setting is enabled on a bearer route only after fail-closed per-call membership checks are deployed on both `/mcp` and `/v1`, with trusted identity forwarding and no bypass path to the backend. Until then the required claims stay enforced.

## Context

The edge cannot know current tenant membership. Relaxing required claims there is safe only when the backend independently refuses every caller that is not a current member, and when nothing can reach the backend without passing the proxy.

## Consequences

- The `/mcp` membership check and a new `/v1` membership check are prerequisites, tracked in the Direct repository.
- Enabling the setting is a separate, reviewed change in the deployment repository, not part of the Identity release.
- Forwarded identity is trusted only from the proxy.

## Required specs and acceptance

- Both routes refuse a valid token whose subject is not a current member (Direct specs).
- A request that bypasses the proxy is refused by the backend.
- The setting stays off until both checks are verified on the deployed build.
