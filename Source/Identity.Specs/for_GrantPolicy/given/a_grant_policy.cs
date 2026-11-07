// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Cratis.Identity.Resources;

namespace Cratis.Identity.for_GrantPolicy.given;

public class a_grant_policy : Specification
{
    private protected GrantPolicy _policy;

    void Establish() => _policy = new GrantPolicy(new ResourceCatalog(Configurations.TwoResourceServers().AsOptions()));
}
