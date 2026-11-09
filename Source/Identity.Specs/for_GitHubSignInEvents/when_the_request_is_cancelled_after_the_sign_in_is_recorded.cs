// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_the_request_is_cancelled_after_the_sign_in_is_recorded : Accounts.SigningIn.given.an_account_adapter
{
    const string ReturnUrl = "/connect/authorize?client_id=cratis-cli";

    OAuthCreatingTicketContext _context;
    ClaimsPrincipal _initial;
    CancellationTokenSource _aborted;
    DefaultHttpContext _http;
    Exception _error;
    RemoteFailureContext _failure;
    int _appended;

    void Establish()
    {
        _aborted = new();
        _account = _account with { Status = AccountStatus.Disabled };

        // The command succeeds and the sign-in is recorded (appended); the caller goes away while the account is read afterwards.
        _commands.Execute(Arg.Any<object>(), Arg.Any<Action<IServiceProvider>>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _appended++;
            return Task.FromResult<CommandResult>(new CommandResult<Contracts.Accounts.AccountSubject> { Response = _subject });
        });
        _readModels.GetInstanceById<Account>(Arg.Any<ReadModelKey>(), default).ReturnsForAnyArgs(async _ =>
        {
            await _aborted.CancelAsync();
            return _account;
        });
    }

    async Task Because()
    {
        using var client = new HttpClient(new ProfileHandler());
        using var token = OAuthTokenResponse.Success(JsonDocument.Parse("{\"access_token\":\"synthetic-test-token\"}"));
        using var user = JsonDocument.Parse("{}");
        _initial = new(new ClaimsIdentity());
        _http = new() { RequestAborted = _aborted.Token };
        var scheme = new AuthenticationScheme("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>));
        _context = new(_initial, new(), _http, scheme, new() { UserInformationEndpoint = "https://api.example.test/user" }, client, token, user.RootElement);
        var events = new GitHubSignInEvents(_adapter, NullLogger<GitHubSignInEvents>.Instance, TimeProvider.System);
        _error = await Catch.Exception(() => events.CreatingTicket(_context));

        // The OAuth handler reports a thrown ticket error as a remote failure.
        _failure = new RemoteFailureContext(_http, scheme, new OAuthOptions(), _error ?? new InvalidOperationException("ticket issued"))
        {
            Properties = new AuthenticationProperties { RedirectUri = ReturnUrl }
        };
        await events.RemoteFailure(_failure);
    }

    void Destroy() => _aborted.Dispose();

    [Fact] void should_have_recorded_the_sign_in() => _appended.ShouldEqual(1);
    [Fact] void should_surface_the_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_issue_a_session_principal() => _context.Principal.ShouldEqual(_initial);
    [Fact] void should_not_report_the_account_as_inactive() => (_error is AccountOperationRefused).ShouldBeFalse();
    [Fact] void should_not_redirect_with_a_denial() => _http.Response.Headers.Location.ToString().ShouldEqual(string.Empty);
    [Fact] void should_handle_the_failure_without_a_response() => _failure.Result.Handled.ShouldBeTrue();

    sealed class ProfileHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = 42, login = "synthetic-login", name = "Synthetic User", email = (string?)null }) });
    }
}
