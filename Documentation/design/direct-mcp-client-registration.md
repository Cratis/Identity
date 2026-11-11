# Direct MCP client registration

```text
Doc id: direct-mcp-client-registration
Version: 1
Status: Draft
```

## Decision

MCP clients of Direct are pre-registered. Each registration has exact IP-loopback redirect URIs and requires PKCE with `S256`. Client ID Metadata Documents ([Identity #59](https://github.com/Cratis/Identity/issues/59)) and Dynamic Client Registration each need their own security review before any work starts.

## Context

The [MCP client compatibility matrix](mcp-client-compatibility-matrix.md) lists how each client registers. Pre-registration is the only approach every listed client supports, and it needs no new network fetch or open registration endpoint on Identity.

## Consequences

- A new client is added by configuration and review, not by self-service.
- Redirect URIs are exact IP literals (`127.0.0.1` or `[::1]`) with the path matched; `localhost` is refused.
- Clients that only support CIMD or DCR are not served until their review is complete.

## Required specs and acceptance

- A registration with a `plain` or missing challenge method is refused.
- A redirect URI using `localhost`, a wildcard or a different path is refused.
- A client that is not pre-registered cannot start an authorization.
