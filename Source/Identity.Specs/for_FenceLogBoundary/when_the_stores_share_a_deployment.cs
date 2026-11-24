// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.for_FenceLogBoundary;

public class when_the_stores_share_a_deployment : Specification
{
    const string Deployment = "65a1b2c3d4e5f60718293a4b";
    const string Other = "75a1b2c3d4e5f60718293a4c";

    static readonly FenceLogOptions _default = new();
    static readonly FenceLogOptions _allowed = new() { AllowSharedDeployment = true };

    [Fact] void should_refuse_it_by_default() => FenceLogBoundary.Refuses(Deployment, Deployment, _default).ShouldBeTrue();

    [Fact] void should_accept_it_when_the_operator_has_said_so() => FenceLogBoundary.Refuses(Deployment, Deployment, _allowed).ShouldBeFalse();

    [Fact] void should_accept_separate_deployments() => FenceLogBoundary.Refuses(Deployment, Other, _default).ShouldBeFalse();

    [Fact] void should_accept_separate_deployments_when_sharing_is_allowed() => FenceLogBoundary.Refuses(Deployment, Other, _allowed).ShouldBeFalse();
}
