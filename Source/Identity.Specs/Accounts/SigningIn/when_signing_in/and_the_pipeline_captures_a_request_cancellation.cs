// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_pipeline_captures_a_request_cancellation : given.an_account_adapter
{
    ServiceProvider _services;
    CancellationTokenSource _request;
    Exception _error;

    void Establish()
    {
        _request = new();
        _services = new ServiceCollection().AddScoped<VerifiedUpstreamSignIn>().BuildServiceProvider();

        // Arc turns a cancellation raised while providing inputs into a failed result instead of throwing.
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns(async _ =>
        {
            await _request.CancelAsync();
            return new CommandResult { ExceptionMessages = ["The operation was canceled."] };
        });
        _adapter = new(new ProtocolCommands(_services.GetRequiredService<IServiceScopeFactory>(), pipeline), new(_readModels));
    }

    async Task Because() => _error = await Catch.Exception(() => _adapter.SignIn(_verified.Authentication, _verified.Profile, _request.Token));

    void Destroy()
    {
        _request.Dispose();
        _services.Dispose();
    }

    [Fact] void should_surface_the_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_report_a_refusal() => (_error is AccountOperationRefused).ShouldBeFalse();
}
