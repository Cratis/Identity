// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_Revocation;

[Collection(IdentityCollection.Name)]
public class when_projection_progress_is_held(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _revocation;
    HttpStatusCode _refresh;
    JsonElement _response;
    bool _consultedProjection;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var (_, tokens) = await browser.RedeemCode(await browser.LoginForCode(challenge), verifier);
        await using var scope = host.Services.CreateAsyncScope();
        var position = await scope.ServiceProvider.GetRequiredService<IObserverProgress>().Position(typeof(Account));
        host.ProjectionProgress.Freeze(typeof(Account), position);
        try
        {
            (_revocation, _) = await browser.Revoke(tokens.GetProperty("refresh_token").GetString());
            (_refresh, _response) = await browser.Refresh(tokens.GetProperty("refresh_token").GetString());
            _consultedProjection = host.ProjectionProgress.Observed.IsCompleted;
        }
        finally
        {
            host.ProjectionProgress.Release();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    [Fact] void should_acknowledge_without_projection_progress() => _revocation.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_deny_refresh_without_projection_progress() => _refresh.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_return_invalid_grant() => _response.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_not_consult_a_projection_as_denial_authority() => _consultedProjection.ShouldBeFalse();
}
