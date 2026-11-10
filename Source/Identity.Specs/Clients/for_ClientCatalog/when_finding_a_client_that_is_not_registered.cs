// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.Clients.for_ClientCatalog;

public class when_finding_a_client_that_is_not_registered : given.a_client_catalog
{
    OpenIddictApplicationDescriptor _unknown;
    OpenIddictApplicationDescriptor _differentCase;
    bool _secret;
    bool _redirect;

    async Task Because()
    {
        _unknown = await Clients.Find("unknown-client");
        _differentCase = await Clients.Find("CRATIS-CLI");
        _secret = await Clients.ValidateSecret("unknown-client", IntrospectionSecret);
        _redirect = await Clients.ValidateRedirect("unknown-client", "http://127.0.0.1:49152/callback");
    }

    [Fact] void should_not_find_it() => _unknown.ShouldBeNull();
    [Fact] void should_not_match_client_ids_case_insensitively() => _differentCase.ShouldBeNull();
    [Fact] void should_not_validate_a_secret_for_it() => _secret.ShouldBeFalse();
    [Fact] void should_not_validate_a_redirect_for_it() => _redirect.ShouldBeFalse();
}
