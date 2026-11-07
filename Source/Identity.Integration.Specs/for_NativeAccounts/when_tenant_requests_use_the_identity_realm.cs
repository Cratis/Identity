// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Specs.TenantHost.Work;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_tenant_requests_use_the_identity_realm(IdentityHost host)
{
    [Theory]
    [InlineData("tenant-alpha")]
    [InlineData("tenant-beta")]
    public async Task should_keep_session_token_and_mine_in_the_realm_while_host_work_remains_tenant_owned(string tenant)
    {
        var browser = host.CreateBrowser();
        using var client = browser.Client;
        client.DefaultRequestHeaders.Add("x-cratis-tenant-id", tenant);
        var argumentsBefore = host.NativeCommands.ArgumentResolutions;
        var handlersBefore = host.NativeCommands.HandlerInvocations;
        var (verifier, challenge) = Browser.Pkce();
        using var consent = await browser.Authorize(Browser.CliAuthorization(challenge));
        consent.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var consentPage = await consent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        consentPage.Contains("Authorize Cratis CLI?", StringComparison.Ordinal).ShouldBeTrue();
        host.NativeCommands.ArgumentResolutions.ShouldBeGreaterThan(argumentsBefore);
        host.NativeCommands.HandlerInvocations.ShouldBeGreaterThan(handlersBefore);

        using var mine = await client.GetAsync("/api/accounts/mine", TestContext.Current.CancellationToken);
        mine.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var mineResult = await mine.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var mineSubject = mineResult.GetProperty("data").GetProperty("id").GetString();
        Guid.TryParse(mineSubject, out _).ShouldBeTrue();

        using var redirect = await browser.Consent(consentPage);
        redirect.StatusCode.ShouldEqual(HttpStatusCode.Redirect);
        var code = QueryHelpers.ParseQuery(redirect.Headers.Location.Query)["code"].ToString();
        code.ShouldNotBeEmpty();
        var (status, tokens) = await browser.RedeemCode(code, verifier);
        status.ShouldEqual(HttpStatusCode.OK);
        var token = await AccessTokens.Validate(browser, tokens.GetProperty("access_token").GetString());
        token.IsValid.ShouldBeTrue();
        token.Claims["sub"].ShouldEqual(mineSubject);
        token.Claims["tid"].ShouldEqual(IdentityHost.MemberTenant);

        var workId = Guid.NewGuid().ToString();
        using var work = await client.PostAsJsonAsync("/api/work/record-tenant-work", new { WorkId = workId }, TestContext.Current.CancellationToken);
        work.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var workResult = await work.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        workResult.GetProperty("isSuccess").GetBoolean().ShouldBeTrue();

        await AssertStoredBoundaries(tenant, mineSubject, workId);
    }

    [Theory]
    [InlineData("tenant-alpha")]
    [InlineData("tenant-beta")]
    public async Task should_refuse_anonymous_mine_without_creating_a_session_or_account_events(string tenant)
    {
        var browser = host.CreateBrowser();
        using var client = browser.Client;
        client.DefaultRequestHeaders.Add("x-cratis-tenant-id", tenant);
        await using var scope = host.Services.CreateAsyncScope();
        var chronicle = scope.ServiceProvider.GetRequiredService<IChronicleClient>();
        var realm = await chronicle.GetEventStore("Identity", EventStoreNamespaceName.Default);
        var tenantStore = await chronicle.GetEventStore("Identity", tenant);
        var realmBefore = await realm.EventLog.GetTailSequenceNumber();
        var tenantBefore = await tenantStore.EventLog.GetTailSequenceNumber();
        var handlersBefore = host.NativeCommands.HandlerInvocations;

        using var response = await client.GetAsync("/api/accounts/mine", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
        host.NativeCommands.HandlerInvocations.ShouldEqual(handlersBefore);
        (await realm.EventLog.GetTailSequenceNumber()).ShouldEqual(realmBefore);
        (await tenantStore.EventLog.GetTailSequenceNumber()).ShouldEqual(tenantBefore);
    }

    async Task AssertStoredBoundaries(string tenant, string subject, string workId)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var chronicle = provider.GetRequiredService<IChronicleClient>();
        var realm = await chronicle.GetEventStore("Identity", EventStoreNamespaceName.Default);
        var tenantStore = await chronicle.GetEventStore("Identity", tenant);
        (await tenantStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        var internalEvents = (await realm.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, subject)).ToArray();
        var registration = internalEvents.Single(@event => @event.Content is AccountRegistrationRecorded);
        await realm.Reactors.WaitTillReachesEventSequenceNumber<AccountPublications>(registration.Context.SequenceNumber, TimeSpan.FromSeconds(10));
        var realmEvents = (await realm.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, subject)).Select(@event => @event.Content).ToArray();
        realmEvents.OfType<AccountRegistrationRecorded>().ShouldContainSingleItem();
        realmEvents.OfType<UpstreamKeySubjectRecorded>().ShouldContainSingleItem();
        realmEvents.OfType<AccountRegistered>().ShouldContainSingleItem();
        var account = await realm.ReadModels.GetInstanceById<Account>(subject);
        account.ShouldNotBeNull();
        account.Id.Value.ShouldEqual(subject);
        (await tenantStore.ReadModels.GetInstanceById<Account>(subject)).ShouldBeNull();
        (await tenantStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, subject)).ShouldBeEmpty();
        (await realm.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, workId)).ShouldBeEmpty();
        var tenantEvents = (await tenantStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, workId)).Select(@event => @event.Content).ToArray();
        tenantEvents.OfType<TenantWorkRecorded>().ShouldContainSingleItem();
        var recorded = tenantEvents.OfType<TenantWorkRecorded>().Single();
        recorded.Subject.Value.ShouldEqual(subject);
        recorded.Tenant.Value.ShouldEqual(tenant);
        using var names = await provider.GetRequiredService<IMongoDatabase>().ListCollectionNamesAsync(cancellationToken: TestContext.Current.CancellationToken);
        (await names.ToListAsync(TestContext.Current.CancellationToken)).Contains("accounts").ShouldBeFalse();
    }
}
