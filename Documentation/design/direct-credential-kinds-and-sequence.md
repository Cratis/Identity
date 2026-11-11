# Direct credential kinds and sequence

```text
Doc id: direct-credential-kinds-and-sequence
Version: 1
Status: Draft
```

## Decision

Direct accepts plain bearer access tokens now and DPoP-bound tokens later. TLS is required end to end, tokens are redacted from logs and storage, and there is no fallback between a personal access token and an OAuth token: a request is judged by the credential kind it presents and never retried as the other kind.

## Context

DPoP gives sender-constrained tokens but needs client and resource support that MCP clients do not yet share. Bearer tokens are the interoperable choice today, which makes transport protection and redaction the controls that matter.

## Consequences

- Access tokens stay short-lived.
- Logging and error paths never include a token value.
- Adding DPoP later is a new decision; it must not weaken the bearer rules for clients that cannot use it without a recorded change.
- The no-fallback rule is tracked in AuthProxy #257.

## Required specs and acceptance

- A failed OAuth validation never falls back to personal access token validation, and the reverse.
- Log and storage paths redact tokens (specs in the owning repositories).
- Plain HTTP is refused on every bearer route.
