// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Browser;

public class when_redirects_loop : Specification
{
    readonly RedirectLoop _loop = new();
    HttpClient _client;
    Exception _error;

    void Establish() => _client = new HttpClient(_loop) { BaseAddress = new Uri("https://identity.example.test") };

    async Task Because()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/start");
        _error = await Catch.Exception(() => new Browser(_client).Send(request));
    }

    [Fact] void should_fail_with_a_clear_exception() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_follow_at_most_twenty_redirects() => _loop.Requests.ShouldEqual(21);
    [Fact] void should_name_the_redirect_limit() => _error.Message.ShouldContain("Identity exceeded 20 redirects");
    [Fact] void should_name_the_redirect_chain() => _error.Message.ShouldContain("/start -> /again -> /again");

    void Destroy() => _client.Dispose();

    sealed class RedirectLoop : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("/again", UriKind.Relative);
            return Task.FromResult(response);
        }
    }
}
