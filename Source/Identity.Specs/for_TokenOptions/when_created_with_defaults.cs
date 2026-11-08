// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_TokenOptions;

public class when_created_with_defaults : Specification
{
    TokenOptions _options;

    void Because() => _options = new TokenOptions();

    [Fact] void should_default_the_access_token_lifetime_to_300_seconds() => _options.AccessTokenLifetime.TotalSeconds.ShouldEqual(300);
}
