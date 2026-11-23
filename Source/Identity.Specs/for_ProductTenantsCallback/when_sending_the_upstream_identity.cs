// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_sending_the_upstream_identity : given.a_tenants_callback
{
    void Establish()
    {
        var selected = account.LinkedUpstreamKeys.Single();
        var other = new LinkedUpstreamKey(new(Guid.NewGuid()), new(Guid.NewGuid()), new("https://issuer.example"), new("external-subject"), new("external-login"), DateTimeOffset.UnixEpoch.AddDays(-1));
        var later = selected with { LinkId = new(Guid.NewGuid()), IdpSub = new("999"), Login = new("later-login"), LinkedAt = DateTimeOffset.UnixEpoch.AddDays(1) };
        account = account with { LinkedUpstreamKeys = [other, later, selected] };
    }

    Task Because() => Lookup();

    [Fact] void should_keep_the_original_audience() => JsonElement.Parse(handler.RequestBody).GetProperty("audience").GetString().ShouldEqual(resource.Audience);
    [Fact] void should_keep_the_canonical_subject() => JsonElement.Parse(handler.RequestBody).GetProperty("subject").GetString().ShouldEqual(account.Id.Value);
    [Fact] void should_keep_the_selected_github_membership_key() => JsonElement.Parse(handler.RequestBody).GetProperty("github_id").GetString().ShouldEqual("501");
    [Fact] void should_send_only_the_three_consumer_fields() => JsonElement.Parse(handler.RequestBody).EnumerateObject().Select(_ => _.Name).ShouldContainOnly("audience", "subject", "github_id");
    [Fact] void should_keep_accepting_the_products_tenant_list() => ((TenantsListed)result).Tenants.Select(_ => _.Id).ShouldContainOnly("one", "two");
}
