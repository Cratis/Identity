// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_TenantsCallbackHttpClient;

public class when_creating_the_handler : Specification
{
    [Fact]
    void should_disable_redirects_and_cookies_and_keep_default_tls_validation()
    {
        using var handler = TenantsCallbackHttpClient.CreateHandler();
        handler.AllowAutoRedirect.ShouldBeFalse();
        handler.UseCookies.ShouldBeFalse();
        handler.SslOptions.RemoteCertificateValidationCallback.ShouldBeNull();
    }
}
