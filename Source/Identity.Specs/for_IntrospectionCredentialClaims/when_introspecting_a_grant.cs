// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.for_IntrospectionCredentialClaims;

public class when_introspecting_a_grant : given.a_validated_access_token
{
    async Task Because() => await Populate();

    [Fact] void should_report_the_oauth_grant_kind() => ((string?)_context.Claims["credential_kind"]).ShouldEqual("oauth-grant");
    [Fact] void should_use_the_event_source_grant_uuid() => ((string?)_context.Claims["credential_id"]).ShouldEqual(OutboxEntry.Source(AuthorizationId).ToString("D"));
    [Fact] void should_not_use_the_token_id() => ((string?)_context.Claims["credential_id"] == _context.TokenId).ShouldBeFalse();
    [Fact] void should_not_use_the_client_id() => ((string?)_context.Claims["credential_id"] == _context.ClientId).ShouldBeFalse();
    [Fact] void should_not_expose_the_operational_authorization_id() => ((string?)_context.Claims["credential_id"] == AuthorizationId).ShouldBeFalse();
    [Fact] void should_report_the_current_recovery_epoch() => ((long?)_context.Claims["cache_epoch"]).ShouldEqual(7L);
    [Fact] void should_use_the_token_tenant_not_the_request() => ((string?)_context.Claims["tid"]).ShouldEqual("token-tenant");
    [Fact] void should_use_the_token_audience_not_the_request() => _context.Audiences.ShouldContainOnly("direct-api");
    [Fact] void should_report_the_granted_scopes() => _context.Scopes.ShouldContainOnly("direct:read", "offline_access");
    [Fact] void should_not_add_confirmation_to_a_bearer_token() => _context.Confirmation.ShouldBeNull();
}
