<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Client ID Metadata Documents

Identity can resolve an HTTPS URL used as an OAuth client identifier without creating a durable client registration. This opt-in feature implements the public-client subset of [Client ID Metadata Documents draft -03](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-client-id-metadata-document-03). It does not implement Dynamic Client Registration (RFC 7591). Compatibility with external MCP connectors remains unverified.

## Enable metadata lookup

Set `Identity:Clients:MetadataDocuments:Enabled` to `true`. The default is `false`: unknown clients are refused and CIMD support is not advertised. Configured clients always take precedence, including configured URL-shaped client identifiers.

```json
{
  "Identity": {
    "Clients": {
      "MetadataDocuments": {
        "Enabled": true,
        "AllowedHosts": ["client.example"],
        "AllowLocalhostLoopback": false
      }
    }
  }
}
```

`AllowedHosts` is an optional exact lowercase ASCII hostname allowlist, not a wildcard or suffix match. An empty list allows any hostname that passes the public-address checks. Public-address checks still apply to allowlisted hosts.

When enabled, both `/.well-known/oauth-authorization-server` and `/.well-known/openid-configuration` advertise `client_id_metadata_document_supported: true` and the `none` token endpoint authentication method. Neither document advertises `registration_endpoint` or `client_registration_types_supported`.

## Supported client profile

The client identifier must be an exact canonical HTTPS URL with a non-root path, no query, fragment, credentials or non-default port. Metadata must be a JSON object whose `client_id` exactly equals that identifier; normalization does not repair a mismatch. Duplicate keys, malformed UTF-8 or Unicode, excessive nesting, and missing or invalid redirects are refused with `invalid_client`.

A minimal synthetic document is:

```json
{
  "client_id": "https://client.example/client.json",
  "client_name": "Example client",
  "redirect_uris": ["https://client.example/callback"],
  "token_endpoint_auth_method": "none",
  "grant_types": ["authorization_code", "refresh_token"],
  "response_types": ["code"]
}
```

Clients are public, require authorization code plus PKCE S256, and always require explicit consent. Refresh tokens are allowed only when the document lists the refresh-token grant; normal issuer policy and `offline_access` still apply. Confidential authentication, secrets, client keys, request objects, implicit flow, device grants and token exchange are not supported through CIMD. Document-supplied scopes and presentation URLs confer no permissions; issuer-owned resource and scope policy remains authoritative.

Web redirects use exact HTTPS matching. Native redirects declare `http://127.0.0.1/callback` or `http://[::1]/callback` without a port; only the requested loopback port may vary. Path, host, scheme and query must still match. Web and native redirects cannot be mixed. `localhost` is refused by default. `Identity:Clients:MetadataDocuments:AllowLocalhostLoopback` explicitly opts into native `http://localhost/callback` declarations; it does not make localhost an IP-literal alias or permit fetching metadata from loopback addresses. The default follows the signed-off MCP contract; enabling this exception is an issuer policy choice pending that contract's amendment.

Consent shows the canonical ASCII hostname and labels the sanitized client-provided name as unverified. For native clients it also warns that any local program can use that loopback client identity. HTTPS metadata proves control of a document host, not the identity of a particular local program.

## Outbound and cache boundaries

Identity fetches only status 200 `application/json` responses. Redirects, cookies, proxy settings, credentials and decompression are disabled. Every DNS answer must be public; any private, loopback, link-local, documentation, special-purpose, mapped or transitional address refuses the entire answer set. The connection is pinned to a checked address while ordinary TLS hostname and certificate validation remain enabled. CIMD's named HTTP client excludes host-wide retry and resilience handlers so a single admitted fetch cannot silently fan out.

Only validated successful documents enter the bounded cross-request cache. Freshness comes from `Cache-Control`, `Age` and `Expires`, or the configured default, within the issuer's TTL limits. `no-store`, `no-cache`, `max-age=0` and expired responses are not reused across requests. A detached snapshot keeps repeated OpenIddict lookups consistent inside a single request, including for non-cacheable responses; the next request must fetch again. Refusals are not negatively cached, so a repaired document can recover immediately. Expired metadata is never served after failed revalidation.

Metadata and reverse application-ID mappings are operational, in-memory state, not durable registration or grant authority. Refresh after a restart supplies the client identifier and revalidates its document; stable application identifiers do not depend on retaining the metadata cache. Normal grant fences, refresh rotation and recovery checks still apply.

## Issuer limits

All settings below are under `Identity:Clients:MetadataDocuments`. Invalid bounds are refused at startup, even when the feature is disabled.

| Setting | Default | Purpose |
| --- | --- | --- |
| `MaxDocumentBytes` | `5120` | Declared and streamed response-body bound; permitted range 1–64 KiB. |
| `ConnectTimeout` | `00:00:03` | DNS and connection deadline within the total fetch deadline. |
| `RequestTimeout` | `00:00:05` | Total fetch deadline including body reads; must be less than ten seconds. |
| `MinCacheTtl` | `00:01:00` | Minimum lifetime for reusable freshness, never for no-cache/no-store responses. |
| `DefaultCacheTtl` | `01:00:00` | Lifetime when the origin supplies no freshness. |
| `MaxCacheTtl` | `1.00:00:00` | Maximum successful cache lifetime. |
| `MaxRedirectUris` | `10` | Maximum declared redirects. |
| `MaxCacheEntries` | `256` | Bound on positive and in-flight cache entries. |
| `MaxConcurrentFetches` | `8` | Parallel outbound fetch bound, without an unbounded queue. |
| `MaxFetchesPerMinute` | `60` | Global outbound admission budget. |
| `MaxFetchesPerHostPerMinute` | `10` | Per-host outbound admission budget. |

Admission limits apply to network fetches, not repeated lookups of a request snapshot or live cached revision. Non-cacheable documents consume a new admission on each protocol request. CIMD diagnostics use Debug logging with bounded reasons, ASCII hosts, lookup duration and cache-hit state, never document bodies, full URLs, resolved addresses or credentials.

See [MCP authorization](../specs/arc-mcp-authorization-v1.md) for the resource-server contract and [the scope catalog](../specs/arc-scope-catalog-v1.md) for issuer-owned permissions.
