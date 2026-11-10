// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ArcMcpVectors;

public class when_redeeming_an_authorization_grant_with_v2_1_0 : when_redeeming_an_authorization_grant_through_issuance
{
    protected override string VectorVersion => "v2.1.0";
}
