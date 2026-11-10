// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_redirect_is_not_loopback : given.a_development_profile
{
    void Establish() => _settings["Identity:Development:Clients:0:RedirectUris:0"] = "https://app.example.com/callback";

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
}
