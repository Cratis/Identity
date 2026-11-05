// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_secret_is_empty_in_development : given.a_tenants_callback
{
    void Establish() => options.Secret = string.Empty;
    Task Because() => Lookup();

    [Fact] void should_omit_authorization() => handler.Requests.Single().Headers.Authorization.ShouldBeNull();
}
