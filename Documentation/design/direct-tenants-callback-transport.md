# Tenants callback transport

```text
Doc id: direct-tenants-callback-transport
Version: 1
Status: Draft
```

## Decision

The tenants callback from Identity to a resource server runs over an isolated internal listener: in-cluster HTTPS with a certificate from a cert-manager private CA, hostname verification, an authenticated callback secret and a NetworkPolicy that admits only Identity. Any certificate or callback failure denies the request. The callback never downgrades to HTTP.

## Context

The callback decides which tenants a caller may enter. Identity already requires an HTTPS URL and a callback secret of at least 32 characters outside Development. The transport must carry the same guarantee end to end.

## Consequences

- Direct exposes the callback only on the internal listener, never on the public route.
- Certificate rotation and trust distribution are handled by the cluster, not by Identity.
- A misconfigured callback fails closed: the caller gets no tenants.

## Required specs and acceptance

- Identity refuses an HTTP callback URL or a short secret in production configuration (existing specs).
- A certificate or hostname failure yields no tenants.
- The NetworkPolicy and listener isolation are verified in the deployment repository.
