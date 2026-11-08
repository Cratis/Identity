// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;

namespace Cratis.Identity.Accounts.for_AccountErasureCoordinator;

public class when_the_configuration_is_fail_closed : given.an_erasure_workflow
{
    string? _notRequested;
    string? _boundaryRefusal;
    string? _scopeRefusal;
    string? _stagesRefusal;
    bool _defaultBoundaryApproved;
    int _defaultNamespaces;

    async Task Establish()
    {
        _notRequested = await Refusal(() => Coordinator.Run(_account, _erasure));
        await Tombstone();
        await Requests.Request(new ClaimsPrincipal(), _account, _erasure);
    }

    async Task Because()
    {
        var approved = _boundary;
        var namespaces = _namespaces;
        var stages = _stages;

        _boundary = null;
        _boundaryRefusal = await Refusal(() => Coordinator.Run(_account, _erasure));
        _boundary = approved;

        _namespaces = [];
        _scopeRefusal = await Refusal(() => Coordinator.Run(_account, _erasure));
        _namespaces = namespaces;

        _stages = stages.Take(4).ToArray();
        _stagesRefusal = await Refusal(() => Coordinator.Run(_account, _erasure));

        _defaultBoundaryApproved = new UnconfiguredErasureBoundaryPolicy().Approved is not null;
        _defaultNamespaces = (await new NoErasureTenantScope().NamespacesFor(_account)).Count;
    }

    [Fact] void should_refuse_an_unrequested_erasure() => _notRequested.ShouldEqual("erasure_not_requested");
    [Fact] void should_refuse_without_an_approved_boundary() => _boundaryRefusal.ShouldEqual("erasure_boundary_unconfigured");
    [Fact] void should_refuse_without_a_tenant_scope() => _scopeRefusal.ShouldEqual("erasure_tenant_scope_unconfigured");
    [Fact] void should_refuse_with_a_missing_stage() => _stagesRefusal.ShouldEqual("erasure_stages_incomplete");
    [Fact] void should_not_execute_any_stage() => _executed.ShouldBeEmpty();
    [Fact] void should_not_append_completion_or_failure() => _log.Facts.Count.ShouldEqual(2);
    [Fact] void should_default_to_no_approved_boundary() => _defaultBoundaryApproved.ShouldBeFalse();
    [Fact] void should_default_to_no_namespaces() => _defaultNamespaces.ShouldEqual(0);
}
