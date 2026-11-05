// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_FactId;

public class when_retrying_an_accepted_operation : Specification
{
    FactId _operation;
    FactId _fact;

    void Establish() => _operation = FactId.New();
    void Because() => _fact = FactId.ForOperation(_operation, nameof(AccountRegistrationRecorded));

    [Fact] void should_keep_the_same_fact_reference_on_retry() => FactId.ForOperation(_operation, nameof(AccountRegistrationRecorded)).ShouldEqual(_fact);
    [Fact] void should_distinguish_another_fact_in_the_same_operation() => FactId.ForOperation(_operation, nameof(UpstreamKeyLinkRecorded)).ShouldNotEqual(_fact);
    [Fact] void should_distinguish_another_operation() => FactId.ForOperation(FactId.New(), nameof(AccountRegistrationRecorded)).ShouldNotEqual(_fact);
}
