# Client ID Metadata Documents

Identity supports an **opt-in, public-client subset** of [OAuth Client ID Metadata Documents, draft -03](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-client-id-metadata-document-03). This is not complete CIMD support, RFC 7591 Dynamic Client Registration, or a claim of compatibility with a particular vendor client. It does not register clients durably or emit registration events.

## Enable the profile

Enable `Identity:Clients:MetadataDocuments:Enabled`. It defaults to `false` in every environment. When disabled, unknown clients do not initiate HTTP requests and discovery does not advertise CIMD. Configured clients retain precedence, including when a configured identifier looks like a metadata URL.

```json
{
  "Identity": {
    "Clients": {
      "MetadataDocuments": {
        "Enabled": true,
        "AllowedHosts": ["client.example"]
      }
    }
  }
}
```

`AllowedHosts` is optional. An empty list permits any host that passes the public-host and DNS-address policies; it does **not** permit private destinations. Entries must be exact canonical lowercase ASCII DNS hosts. There are no wildcard or suffix matches.

When enabled, both authorization-server discovery documents advertise `client_id_metadata_document_supported: true` and include `none` in `token_endpoint_auth_methods_supported`. Identity does not advertise `registration_endpoint` or `client_registration_types_supported`.

## Supported document subset

The request's `client_id` is the exact metadata URL. A public web example:

```json
{
  "client_id": "https://client.example/client.json",
  "client_name": "Example client",
  "redirect_uris": ["https://client.example/callback"],
  "token_endpoint_auth_methods_supported": ["none"],
  "grant_types": ["authorization_code", "refresh_token"],
  "response_types": ["code"]
}
```

The older singular `token_endpoint_auth_method: "none"` spelling is also accepted. If both spellings are present, both must declare only `none`. An omitted authentication method defaults to `none`. Other methods, including `private_key_jwt`, are refused with the internal `UnsupportedAuthMethod` reason; no key URL is fetched.

Only authorization code and optional refresh grants are admitted. Refresh permission requires `refresh_token` in the document's `grant_types`; it is not inferred from a scope. Consent must still authorize `offline_access` for a refresh token to be issued. An omitted response type means `code`; other response types are refused. Every admitted client is public, requires S256 PKCE and explicit consent, and receives no introspection, device, exchange or client-credentials permission.

**The document's `scope` is ignored.** Permissions come from Identity's registered resource and scope catalogue, not from remote text. Existing resource selection, audience binding, account/tenant checks, consent, code redemption, refresh rotation, fences, revocation and recovery gates remain in effect. An unknown resource or scope does not become usable by appearing in metadata.

Embedded secrets, registration access tokens, JWKS/JWKS URLs, request URI registrations, software statements and signing/encryption declarations outside this public profile are refused. `logo_uri`, `policy_uri`, `tos_uri`, `contacts` and unrelated extensions are ignored: they are not fetched, linked, stored in durable events, or rendered. Names are presentation only: controls, bidi and zero-width formatting are removed, whitespace is collapsed, and text is limited to 100 characters before HTML encoding.

## Web and native redirects

Every redirect declaration must be valid; invalid entries are never silently dropped. The set must be entirely web or entirely native. Empty, duplicate, mixed, over-limit or application-type-inconsistent sets are refused.

Web declarations must be canonical public-host HTTPS URLs, with no credentials, non-default port, query or fragment. OpenIddict's exact web-redirect matcher is retained: no port exception is granted to a web client.

A native document declares HTTP loopback IP literals **without a port**:

```json
{
  "client_id": "https://client.example/native.json",
  "redirect_uris": ["http://127.0.0.1/callback", "http://[::1]/callback"],
  "token_endpoint_auth_method": "none",
  "grant_types": ["authorization_code", "refresh_token"]
}
```

OpenIddict's native matcher permits an ephemeral port at authorization time; scheme, host, path and query still have to match. The authorization code remains bound to the actual redirect URI, including its chosen port, at redemption. Alternate numeric IP spellings, custom schemes, HTTPS loopback and explicitly ported CIMD declarations are refused.

`AllowLocalhostLoopback` defaults to `false`. Its opt-in exception accepts only literal `http://localhost/path` declarations for **native public CIMD clients**, never metadata fetch destinations, web clients or configured legacy clients. **Pending spec amendment (woksin):** the accepted RFC 8252 profile discourages `localhost`; prefer `127.0.0.1` or `[::1]`. This flag is independent of development mode.

## Network boundary

A client identifier must be at most 2048 ASCII characters and be canonical HTTPS on port 443, with a non-root path. Credentials, queries, fragments, trailing-dot hosts, Unicode hosts, single-label/internal/local names, IP literals and alternate IP spellings are refused before HTTP. Unicode names must use their canonical ASCII punycode form. Non-canonical scheme/host casing, explicit `:443`, dot segments and normalization-dependent forms are refused rather than rewritten. The document's `client_id` must equal the original request string ordinally; normalized equivalence is not identity.

The dedicated named HTTP client has no redirects, retries, cookies, credentials, ambient proxy, automatic decompression or default request logging. Only a 200 `application/json` UTF-8 response is admitted. Declared length and streamed bytes are bounded independently. Duplicate keys at every level, excessive depth, invalid UTF-8 and non-object JSON roots are refused.

DNS is resolved inside `ConnectCallback`, once per new connection. **Every answer** must be public unicast before any connection is attempted. A mixed public/private answer is refused as a whole. The connector receives a checked IP address, never the hostname, so there is no second DNS lookup between the check and connection. Loopback, private, link-local, unspecified, multicast, documentation/reserved, carrier-grade NAT, IPv4-mapped/compatible and translation/tunnel IPv6 ranges are refused. The original hostname remains the TLS SNI and certificate-validation target.

TLS versions are left at the targeted .NET/OS defaults (`SslProtocols.None`), relying on that platform's TLS 1.2+ policy. No certificate-validation callback is installed and chain/hostname validation is never bypassed. Protocol negotiation is not inspected from a certificate callback: it is not a reliable post-authentication negotiation boundary. Keep the host platform's TLS policy current. This is not a separate application-enforced TLS-version floor.

## Limits and freshness

Settings below are under `Identity:Clients:MetadataDocuments` and are validated at startup even when disabled.

| Setting | Default | Accepted bound |
| --- | --- | --- |
| `MaxDocumentBytes` | 5120 | 1024–65536 |
| `ConnectTimeout` | 3 seconds | Positive, no greater than `RequestTimeout`; includes DNS |
| `RequestTimeout` | 5 seconds | Positive, strictly less than 10 seconds; includes body reads |
| `MaxRedirectUris` | 10 | 1–10 |
| `MinCacheTtl` | 1 minute | Positive, no greater than the default TTL |
| `DefaultCacheTtl` | 1 hour | Between minimum and maximum TTL |
| `MaxCacheTtl` | 24 hours | Positive, at most 24 hours |
| `MaxCacheEntries` | 256 | 1–4096, including in-flight reservations |
| `MaxConcurrentFetches` | 8 | 1–32, no greater than entry capacity |
| `MaxFetchesPerMinute` | 60 | 1–10000 globally |
| `MaxFetchesPerHostPerMinute` | 10 | Positive, no greater than the global limit |

Positive cache keys are exact canonical client IDs. Cache-Control `max-age` (with Age), or Expires, is bounded by the TTL policy; absent freshness uses the default. `no-store`, `no-cache`, zero/stale freshness and malformed Cache-Control prevent reuse, overriding the minimum TTL. No response body or failure result is negative-cached. Repaired invalid metadata can therefore succeed on the next admitted request. A separate bounded minute-window admission limiter can still refuse requests until capacity returns; it is not a negative document cache.

Concurrent requests for the same key share one bounded fetch. A cancelled waiter does not cancel other waiters. New keys are refused rather than queued without bound when entries or concurrency are exhausted. Only completed entries are evicted. Returned descriptors are detached mutable copies. Failed revalidation never serves a stale descriptor.

OpenIddict entity caching is disabled when this opt-in profile is enabled: otherwise its cached misses and applications could outlive the metadata policy. Configured clients still resolve first. CIMD application IDs retain the existing deterministic 24-hex hash of the exact client ID; an observed collision is fatal. Reverse lookup is bounded to already validated live entries and never manufactures a URL from an unknown operational ID. Following cache loss, token requests re-resolve their original client ID and recover the same application ID. Missing, refused or unavailable metadata fails closed rather than synthesizing a permissive registration.

## Consent, audit and compatibility

Consent prominently separates the canonical ASCII client hostname/URL from the **name provided by the client (not verified)**. Native loopback consent warns that any local program can use the identifier: the metadata hostname does not prove which local process is asking. Names are encoded plain text, never links or verified publisher claims. Configured-client wording is unchanged.

Feature logs contain only the canonical host, a bounded outcome, timing, cache-hit state and the `ClientIdentifierMetadataDocument` consent registration kind. They contain no remote body, optional URL, query, authorization header, secret, token, untrusted name or resolved address. Existing durable consent-event and registration-event schemas are unchanged; no cross-repository Contracts version is needed.

Unit specs exercise URL/address boundaries, transport flags, parsing, bounded caching/admission, real manager freshness/redirect matching, real authorization validators and presentation. Integration/conformance specs use the real issuer and stores with only metadata HTTP replaced; they cover discovery, issuance, repair and refresh after a fresh provider. **Integration execution is CI-only.** Browser/vendor-client interoperability remains unverified. See the [MCP compatibility matrix](design/mcp-client-compatibility-matrix.md) and [conformance catalogue](conformance.md).
