// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients;

public class when_reserving_client_ids : given.client_commands
{
    [Fact]
    async Task should_preserve_case_sensitive_exact_identifier_reservations()
    {
        using var scenario = new EventScenario();
        await scenario.Given.ForEventSource(_id.Value).Events(_configuration.Registered(new("Case-client"), Now));
        var distinct = await scenario.When.ForEventSource(ClientRegistrationId.New().Value).Events(_configuration.Registered(new("case-client"), Now));
        distinct.Errors.ShouldBeEmpty();
        distinct.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    async Task should_allow_only_one_of_two_simultaneous_registration_batches()
    {
        using var scenario = new EventScenario();
        var command = new RegisterClient(new("racing-client"), _configuration, Plaintext);
        var first = command.Handle(_registrationDecision, _digester).AsT0.ToArray();
        var second = command.Handle(_registrationDecision, _digester).AsT0.ToArray();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = 0;
        async Task WaitForStart()
        {
            if (Interlocked.Increment(ref entered) == 2)
            {
                ready.SetResult();
            }

            await start.Task.WaitAsync(TestContext.Current.CancellationToken);
        }

        var firstAttempt = Task.Run(
            async () =>
            {
                await WaitForStart();
                return await scenario.When.ForEventSource(first[0].EventSourceId).Events(first[0].Event, first.Skip(1).Select(item => item.Event).ToArray());
            },
            TestContext.Current.CancellationToken);
        var secondAttempt = Task.Run(
            async () =>
            {
                await WaitForStart();
                return await scenario.When.ForEventSource(second[0].EventSourceId).Events(second[0].Event, second.Skip(1).Select(item => item.Event).ToArray());
            },
            TestContext.Current.CancellationToken);
        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, TestContext.Current.CancellationToken);
        }
        finally
        {
            start.TrySetResult();
        }
        var results = await Task.WhenAll(firstAttempt, secondAttempt);
        results.SelectMany(result => result.Errors).ShouldBeEmpty();
        results.Count(result => result.IsSuccess).ShouldEqual(1);
        results.Count(result => result.HasConstraintViolations).ShouldEqual(1);
    }

    [Fact]
    async Task should_refuse_duplicate_ids_even_after_retirement()
    {
        using var scenario = new EventScenario();
        await scenario.Given.ForEventSource(_id.Value).Events(_configuration.Registered(new("reserved-client"), Now));
        var duplicate = await scenario.When.ForEventSource(ClientRegistrationId.New().Value).Events(_configuration.Registered(new("reserved-client"), Now));
        duplicate.Errors.ShouldBeEmpty();
        duplicate.HasConstraintViolations.ShouldBeTrue();
        await scenario.Given.ForEventSource(_id.Value).Events(new ClientRetired("owner_retired", Now));
        var retired = await scenario.When.ForEventSource(ClientRegistrationId.New().Value).Events(_configuration.Registered(new("reserved-client"), Now));
        retired.Errors.ShouldBeEmpty();
        retired.HasConstraintViolations.ShouldBeTrue();
        var distinct = await scenario.When.ForEventSource(ClientRegistrationId.New().Value).Events(_configuration.Registered(new("distinct-client"), Now));
        distinct.Errors.ShouldBeEmpty();
        distinct.IsSuccess.ShouldBeTrue();
    }
}
