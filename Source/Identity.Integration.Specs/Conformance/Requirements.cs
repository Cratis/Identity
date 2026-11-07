// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.Conformance;

/// <summary>
/// The protocol conformance catalogue. Identifiers are stable across consumers and upgrades.
/// </summary>
public static class Requirements
{
    public const string PkceRequired = "PKCE-REQUIRED";
    public const string PkceS256Only = "PKCE-S256-ONLY";
    public const string PkceVerifierBound = "PKCE-VERIFIER-BOUND";
    public const string RedirectExactLoopback = "REDIRECT-EXACT-LOOPBACK";
    public const string StateAndIssInResponse = "STATE-AND-ISS-IN-RESPONSE";
    public const string AccessTokenAudience = "ACCESS-TOKEN-AUDIENCE";
    public const string ResourceUnknownRefused = "RESOURCE-UNKNOWN-REFUSED";
    public const string ResourceSingleAudience = "RESOURCE-SINGLE-AUDIENCE";
    public const string RefreshRotation = "REFRESH-ROTATION";
    public const string RefreshReuseRevokes = "REFRESH-REUSE-REVOKES";
    public const string RevocationRevokesGrant = "REVOCATION-REVOKES-GRANT";
    public const string IntrospectionInactiveAfterRevoke = "INTROSPECTION-INACTIVE-AFTER-REVOKE";
    public const string IntrospectionAuthenticated = "INTROSPECTION-AUTHENTICATED";
    public const string AsMetadata = "AS-METADATA";
    public const string JwksPublicOnly = "JWKS-PUBLIC-ONLY";
    public const string CodeSingleUse = "CODE-SINGLE-USE";
    public const string CodeRedirectBound = "CODE-REDIRECT-BOUND";
    public const string NoImplicit = "NO-IMPLICIT";
    public const string NoPasswordGrant = "NO-PASSWORD-GRANT";
    public const string TokenResponseNoStore = "TOKEN-RESPONSE-NO-STORE";
    public const string RevocationUnknownToken = "REVOCATION-UNKNOWN-TOKEN";
    public const string AsMetadataFromPrm = "AS-METADATA-FROM-PRM";
    public const string KeyRotationOverlap = "KEY-ROTATION-OVERLAP";
    public const string DeviceLifecycle = "DEVICE-LIFECYCLE";
    public const string DevicePolling = "DEVICE-POLLING";
    public const string DeviceDenial = "DEVICE-DENIAL";
    public const string DeviceSingleUse = "DEVICE-SINGLE-USE";
    public const string DeviceBinding = "DEVICE-BINDING";
    public const string DPoPIssuance = "DPOP-BOUND-ISSUANCE";
    public const string DPoPRefresh = "DPOP-REFRESH-BINDING";
    public const string DPoPNonce = "DPOP-AS-NONCE";
    public const string DPoPReplay = "DPOP-PROOF-REPLAY";
    public const string DPoPProof = "DPOP-PROOF-VALIDATION";
    public const string DPoPMetadata = "DPOP-METADATA";

    public static IReadOnlyList<Requirement> All { get; } =
    [
        new(DPoPIssuance, "RFC 9449 §5 and §6; identity-dpop-v1 §2", "MUST", "When enabled, bind code and approved device issuance to the proof key with object-valued cnf and DPoP token type."),
        new(DPoPRefresh, "RFC 9449 §5; identity-dpop-v1 §2.3", "MUST", "Preserve immutable family binding; refuse missing or different keys without consuming the entry."),
        new(DPoPNonce, "RFC 9449 §8; identity-dpop-v1 §5", "MUST", "Challenge with a stateless AS nonce without consuming the grant."),
        new(DPoPReplay, "RFC 9449 §4.3; identity-dpop-v1 §4", "MUST", "Refuse proof replay across token requests without consuming the current refresh entry."),
        new(DPoPProof, "RFC 9449 §4.3; identity-dpop-v1 §3", "MUST", "Validate ES256 proofs against the exact configured public token endpoint."),
        new(DPoPMetadata, "RFC 9449 §5.1", "MUST", "Advertise ES256 only when the profile is enabled."),
        new(DeviceLifecycle, "RFC 8628 §3.2 and §3.5", "MUST", "Issue bounded device codes and exchange an explicitly approved request."),
        new(DevicePolling, "RFC 8628 §3.5", "MUST", "Keep unapproved requests pending and enforce polling slowdown across connections."),
        new(DeviceDenial, "RFC 8628 §3.5", "MUST", "Refuse device requests denied by the user."),
        new(DeviceSingleUse, "RFC 8628 §3.5; RFC 6749 §5.2", "MUST", "Refuse repeated device-code redemption."),
        new(DeviceBinding, "RFC 8628 §3.5; RFC 6749 §5.2", "MUST", "Bind device codes to their initiating client."),
        new(PkceRequired, "RFC 9700 §2.1.1; RFC 7636 §4.4.1", "MUST", "Require PKCE for public authorization-code clients."),
        new(PkceS256Only, "RFC 7636 §4.2; RFC 9700 §2.1.1", "MUST", "Use S256, never plain PKCE."),
        new(PkceVerifierBound, "RFC 7636 §4.6", "MUST", "Bind redemption to the PKCE verifier."),
        new(RedirectExactLoopback, "RFC 8252 §7.3 and §8.4; RFC 9700 §2.1", "MUST", "Match redirect URIs exactly except the native loopback port; use IP literals."),
        new(StateAndIssInResponse, "RFC 6749 §4.1.2; RFC 9207 §2", "MUST", "Echo state and identify the issuer in authorization responses."),
        new(AccessTokenAudience, "RFC 9068 §2.2 and §4; RFC 8707 §2", "MUST", "Issue a signed at+jwt access token for the selected audience."),
        new(ResourceUnknownRefused, "RFC 8707 §2 (invalid_target)", "MUST", "Refuse unknown resource indicators."),
        new(ResourceSingleAudience, "RFC 8707 §2; issuer single-audience policy", "MUST", "Do not mix audiences or switch resource during code redemption."),
        new(RefreshRotation, "RFC 9700 §4.14.2", "MUST", "Rotate public-client refresh tokens."),
        new(RefreshReuseRevokes, "RFC 9700 §4.14.2", "MUST", "Detect refresh replay and revoke the active grant, including racing successors."),
        new(RevocationRevokesGrant, "RFC 7009 §2.1", "SHOULD", "Revoking a refresh token revokes the grant's access and refresh tokens."),
        new(IntrospectionInactiveAfterRevoke, "RFC 7662 §2.2", "MUST", "Report revoked access tokens as inactive."),
        new(IntrospectionAuthenticated, "RFC 7662 §2.1", "MUST", "Authenticate and authorize introspection callers."),
        new(AsMetadata, "RFC 8414 §2 and §3.3", "MUST", "Publish issuer, endpoints and the supported code-only protocol profile."),
        new(JwksPublicOnly, "RFC 7517 §5; RFC 7518 §6", "MUST", "Publish signing keys without private or symmetric key material."),
        new(CodeSingleUse, "RFC 6749 §4.1.2", "MUST", "Reject repeated authorization-code redemption."),
        new(CodeRedirectBound, "RFC 6749 §4.1.3", "MUST", "Bind the code to the original redirect URI, including its port."),
        new(NoImplicit, "RFC 9700 §2.1.2", "SHOULD", "Do not issue access tokens in authorization responses."),
        new(NoPasswordGrant, "RFC 9700 §2.4", "MUST", "Do not support the resource owner password credentials grant."),
        new(TokenResponseNoStore, "RFC 6749 §5.1", "MUST", "Prevent token response caching and publish Bearer token type and lifetime."),
        new(RevocationUnknownToken, "RFC 7009 §2.2", "MUST", "Return success when revoking an unknown token."),
        new(AsMetadataFromPrm, "RFC 9728 §3; RFC 8414 §3.1 and §3.3", "MUST", "Discover the issuer from a protected resource and validate a token for that resource."),
        new(KeyRotationOverlap, "RFC 8414 §2 (jwks_uri); operational overlap policy", "SHOULD", "Keep old and new signing keys available throughout a rotation overlap window.")
    ];
}
