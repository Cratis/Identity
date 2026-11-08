// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Cratis.Identity.Resources;

namespace Cratis.Identity.for_AccessTokenIdentity.given;

public class an_account_and_a_grant : Specification
{
    protected const string ClientId = "cratis-cli";
    private protected Account _account;
    private protected Grant _grant;

    void Establish()
    {
        _account = new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Einar Ingebrigtsen", null, "134365", "einari", AccountStatus.Active, []);
        var direct = new ResourceCatalog(Configurations.TwoResourceServers().AsOptions()).FindByAudience(Configurations.DirectAudience);
        _grant = new Grant(direct, ["direct:read", "offline_access"]);
    }
}
