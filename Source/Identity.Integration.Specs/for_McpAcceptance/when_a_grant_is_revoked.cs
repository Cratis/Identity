// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_McpAcceptance;

public class when_a_grant_is_revoked : Specification
{
    public static bool ProductEdgeAvailable => false;

    [Fact(SkipUnless = nameof(ProductEdgeAvailable), Skip = "Pending Direct#2660 and Identity#126: both product/edge paths must reject credentials for revoked grants within the published 30-second bound; the JWT-only local validators have no revocation cache.")]
    void should_refuse_revoked_grant_credentials_at_both_products_within_thirty_seconds() =>
        throw new NotSupportedException("Requires the product/edge revocation path; local JWT validation is not evidence of revocation freshness.");
}
