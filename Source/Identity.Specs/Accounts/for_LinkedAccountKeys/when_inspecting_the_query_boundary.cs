// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.for_LinkedAccountKeys;

public class when_inspecting_the_query_boundary : Specification
{
    MethodInfo _method;
    string[] _fields;

    void Because()
    {
        _method = typeof(LinkedAccountKeys).GetMethod(nameof(LinkedAccountKeys.ForCurrentAccount));
        _fields = typeof(LinkedAccountKeys).GetProperties().Select(property => property.Name).ToArray();
    }

    [Fact] void should_require_authorization() => _method.GetCustomAttribute<AuthorizeAttribute>().ShouldNotBeNull();
    [Fact] void should_accept_only_server_dependencies() => _method.GetParameters().Select(parameter => parameter.ParameterType).ShouldEqual([typeof(IHttpContextAccessor), typeof(IReadModels), typeof(IOptions<IdentityOptions>)]);
    [Fact] void should_expose_only_the_owner_view() => _fields.ShouldEqual(["LinkId", "ConnectionId", "IdpIssuer", "DisplayIdentifier", "LinkedAt", "State"]);
}
