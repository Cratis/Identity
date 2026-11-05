// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_a_callback_url_with_user_info : given.a_callback_configuration
{
    [Theory]
    [InlineData("https://user:password@products.example/tenants")]
    [InlineData("https://products.example/tenants#fragment")]
    [InlineData("/tenants")]
    [InlineData("file:///tenants")]
    void should_refuse_unsafe_urls(string url)
    {
        options.Resources[0].TenantsCallback.Url = url;
        Validate();
        result.Failed.ShouldBeTrue();
    }
}
