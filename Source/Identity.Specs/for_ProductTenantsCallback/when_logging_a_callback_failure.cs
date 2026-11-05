// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_logging_a_callback_failure : given.a_tenants_callback
{
    string _logged;

    void Establish()
    {
        options.Url = "https://products.example/tenants?credential=private-query";
        handler.Body = "private-response-body";
    }

    async Task Because()
    {
        await Lookup();
        _logged = string.Join(' ', logger.Messages);
    }

    [Fact] void should_not_log_exceptions() => logger.Exceptions.ShouldBeEmpty();
    [Fact] void should_log_the_category() => _logged.ShouldContain("Malformed");
    [Fact] void should_not_log_the_secret() => _logged.Contains(options.Secret, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_log_the_url() => _logged.Contains(options.Url, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_log_the_response_body() => _logged.Contains(handler.Body, StringComparison.Ordinal).ShouldBeFalse();
}
