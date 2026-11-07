// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_opt_out_is_not_set : Specification
{
    bool _value;

    void Because() => _value = new IdentityOptions().Accounts.ConfirmNoLegacyMongoStore;

    [Fact] void should_default_to_refusing() => _value.ShouldBeFalse();
}
