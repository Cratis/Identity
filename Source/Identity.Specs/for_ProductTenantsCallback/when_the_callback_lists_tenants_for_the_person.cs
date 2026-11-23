// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_lists_tenants_for_the_person : given.a_tenants_callback
{
    Task Because() => Lookup();

    [Fact] void should_list_exactly_the_product_tenants() => ((TenantsListed)result).Tenants.Select(_ => _.Id).ShouldContainOnly("one", "two");
    [Fact] void should_post_to_the_configured_url() => handler.Requests.Single().RequestUri.AbsoluteUri.ShouldEqual(options.Url);
    [Fact] void should_use_post() => handler.Requests.Single().Method.ShouldEqual(HttpMethod.Post);
    [Fact] void should_authenticate() => handler.Requests.Single().Headers.Authorization.ToString().ShouldEqual($"Bearer {options.Secret}");
    [Fact] void should_accept_json() => handler.Requests.Single().Headers.Accept.Single().MediaType.ShouldEqual("application/json");
    [Fact] void should_send_the_audience() => JsonElement.Parse(handler.RequestBody).GetProperty("audience").GetString().ShouldEqual(resource.Audience);
    [Fact] void should_send_the_subject() => JsonElement.Parse(handler.RequestBody).GetProperty("subject").GetString().ShouldEqual(account.Id.Value);
    [Fact] void should_send_the_legacy_github_id() => JsonElement.Parse(handler.RequestBody).GetProperty("github_id").GetString().ShouldEqual("501");
    [Fact] void should_send_exactly_the_three_consumer_fields() => JsonElement.Parse(handler.RequestBody).EnumerateObject().Select(_ => _.Name).ShouldContainOnly("audience", "subject", "github_id");
    [Fact] void should_not_put_the_subject_in_the_url() => handler.Requests.Single().RequestUri.AbsoluteUri.Contains(account.Id.Value, StringComparison.Ordinal).ShouldBeFalse();
}
