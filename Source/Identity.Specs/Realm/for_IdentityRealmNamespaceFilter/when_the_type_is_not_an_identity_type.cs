// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Realm.for_IdentityRealmNamespaceFilter;

public class when_the_type_is_not_an_identity_type : Specification
{
    CommandResult _command;
    QueryResult _query;

    async Task Because()
    {
        var filter = new IdentityRealmNamespaceFilter(NullLogger<IdentityRealmNamespaceFilter>.Instance);
        _command = await filter.OnExecution(new(CorrelationId.New(), typeof(object), new object(), [], new()));
        _query = await filter.OnPerform(new("Example.Other.Mine", CorrelationId.New(), Paging.NotPaged, Sorting.None));
    }

    [Fact] void should_pass_the_command() => _command.IsSuccess.ShouldBeTrue();
    [Fact] void should_pass_the_query() => _query.IsSuccess.ShouldBeTrue();
}
