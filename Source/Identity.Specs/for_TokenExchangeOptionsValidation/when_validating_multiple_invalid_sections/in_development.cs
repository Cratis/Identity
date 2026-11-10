// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_TokenExchangeOptionsValidation.when_validating_multiple_invalid_sections;

public class in_development : given.multiple_invalid_issuers_and_clients
{
    void Because() => Validate(true);

    [Fact] void should_preserve_every_failure_in_duplicate_issuer_and_client_order() => failures.ShouldEqual(Expected(true));
}
