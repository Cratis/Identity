// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Identity.Authorization.ConnectedApps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_inspecting_the_query_boundary : Specification
{
    MethodInfo _method;

    void Because() => _method = typeof(ConnectedApp).GetMethod(nameof(ConnectedApp.ForCurrentAccount));

    [Fact] void should_be_a_discoverable_read_model() => typeof(ConnectedApp).GetCustomAttribute<ReadModelAttribute>().ShouldNotBeNull();
    [Fact] void should_require_authorization() => _method.GetCustomAttribute<AuthorizeAttribute>().ShouldNotBeNull();
    [Fact] void should_accept_only_server_dependencies() => _method.GetParameters().Select(parameter => parameter.ParameterType).ShouldEqual([typeof(IHttpContextAccessor), typeof(ConnectedAppReader)]);
}
