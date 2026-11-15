// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_posting_the_protocol_command_over_http(IdentityHost host)
{
    [Theory]
    [InlineData(false, false, "tenant-alpha")]
    [InlineData(false, true, "tenant-alpha")]
    [InlineData(true, false, "tenant-alpha")]
    [InlineData(true, true, "tenant-alpha")]
    [InlineData(false, false, "tenant-beta")]
    [InlineData(false, true, "tenant-beta")]
    [InlineData(true, false, "tenant-beta")]
    [InlineData(true, true, "tenant-beta")]
    public async Task should_refuse_before_native_handlers_without_appending_events(bool authenticated, bool validate, string tenant)
    {
        var browser = host.CreateBrowser();
        using var client = browser.Client;
        client.DefaultRequestHeaders.Add("x-cratis-tenant-id", tenant);
        if (authenticated)
        {
            using var consent = await browser.Authorize(Browser.CliAuthorization(Browser.Pkce().Challenge));
            consent.StatusCode.ShouldEqual(HttpStatusCode.OK);
            using var mine = await client.GetAsync("/api/accounts/mine", TestContext.Current.CancellationToken);
            mine.StatusCode.ShouldEqual(HttpStatusCode.OK);
        }

        await using var scope = host.Services.CreateAsyncScope();
        var chronicle = scope.ServiceProvider.GetRequiredService<IChronicleClient>();
        var realm = await chronicle.GetEventStore("Identity", EventStoreNamespaceName.Default);
        var tenantStore = await chronicle.GetEventStore("Identity", tenant);
        var before = await realm.EventLog.GetTailSequenceNumber();
        var tenantBefore = await tenantStore.EventLog.GetTailSequenceNumber();
        var argumentsBefore = host.NativeCommands.ArgumentResolutions;
        var handlersBefore = host.NativeCommands.HandlerInvocations;
        var route = "/api/accounts/signing-in/sign-in-with-upstream-key" + (validate ? "/validate" : string.Empty);
        using var response = await client.PostAsJsonAsync(route, new { OperationId = FactId.New().Value }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
        host.NativeCommands.ArgumentResolutions.ShouldEqual(argumentsBefore);
        host.NativeCommands.HandlerInvocations.ShouldEqual(handlersBefore);

        // Recovery and publication workers append their own facts concurrently, so the global tail is not stable; no account event may appear.
        var accountEventTypes = typeof(Contracts.Accounts.Events.AccountEnabled).Assembly.GetTypes()
            .Where(_ => _.Namespace?.StartsWith("Cratis.Identity.Contracts.Accounts", StringComparison.Ordinal) == true)
            .Select(_ => _.GetCustomAttributes(typeof(EventTypeAttribute), false).Cast<EventTypeAttribute>().FirstOrDefault()?.Id.ToString())
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = (await realm.EventLog.GetFromSequenceNumber(before.Next())).Concat(await tenantStore.EventLog.GetFromSequenceNumber(tenantBefore.Next()));
        added.Where(_ => accountEventTypes.Contains(_.Context.EventType.Id.ToString())).ShouldBeEmpty();
        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }
}
