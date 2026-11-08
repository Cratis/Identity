// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Accounts;
using Cratis.Identity.Protocol;
using NSubstitute.Extensions;

namespace Cratis.Identity.Architecture;

public class when_inspecting_account_projection_inputs : Specification
{
    Type[] _actual;

    void Because()
    {
        var builder = Substitute.For<IProjectionBuilderFor<Account>>();
        builder.ReturnsForAll(builder);
        var children = Substitute.For<IChildrenBuilder<Account, LinkedUpstreamKey>>();
        children.ReturnsForAll(children);
        builder.Children<LinkedUpstreamKey>(default!, default!).ReturnsForAnyArgs(call =>
        {
            call.ArgAt<Action<IChildrenBuilder<Account, LinkedUpstreamKey>>>(1)(children);
            return builder;
        });
        builder.ClearReceivedCalls();
        new AccountProjection().Define(builder);
        builder.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Children").ShouldEqual(1);
        _actual = builder.ReceivedCalls().Concat(children.ReceivedCalls())
            .Where(call => call.GetMethodInfo().Name == "From" || call.GetMethodInfo().Name == "Join" || call.GetMethodInfo().Name == "RemovedWith")
            .Select(call => call.GetMethodInfo().GetGenericArguments()[0]).Distinct().OrderBy(type => type.FullName).ToArray();
    }

    [Fact] void should_match_the_event_types_used_by_define() => _actual.SequenceEqual(ProjectionInputs.For(typeof(Account)).OrderBy(type => type.FullName)).ShouldBeTrue();
}
