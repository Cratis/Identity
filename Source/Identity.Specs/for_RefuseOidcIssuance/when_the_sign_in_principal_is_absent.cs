// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RefuseOidcIssuance;

public class when_the_sign_in_principal_is_absent : Specification
{
    ProcessSignInContext _context;

    void Establish() => _context = new(new OpenIddictServerTransaction { Options = new(), Request = new(), Response = new() });

    async Task Because() => await new RefuseOidcIssuance(Options.Create(new IdentityOptions()), TimeProvider.System).HandleAsync(_context);

    [Fact] void should_leave_missing_principal_admission_to_the_native_handler() => _context.IsRejected.ShouldBeFalse();
    [Fact] void should_not_generate_an_identity_token() => _context.GenerateIdentityToken.ShouldBeFalse();
}
