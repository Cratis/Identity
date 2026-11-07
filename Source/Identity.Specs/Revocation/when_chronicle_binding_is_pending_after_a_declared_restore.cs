// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_chronicle_binding_is_pending_after_a_declared_restore : given.a_recovery_pass
{
    readonly TaskCompletionSource _binding = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _readyWhileBinding;

    void Establish()
    {
        _contains = false;
        _options.RestoreKind = RestoreKind.Both;
        _options.RestoreId = "chronicle-restore-binding";
        _options.RestrictionsReestablished = true;
        _facts.Bind(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            _binding.SetResult();
            await _resume.Task.WaitAsync(TimeSpan.FromSeconds(5));
            throw new RevocationPending();
        });
    }

    async Task Because()
    {
        var pass = _recovery.Run(CancellationToken.None);
        await _binding.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _readyWhileBinding = _gate.Ready;
        _resume.SetResult();
        _failure = await Catch.Exception(() => pass);
    }

    [Fact] void should_repair_operational_denials_before_chronicle_binding() => _contains.ShouldBeTrue();
    [Fact] void should_not_admit_while_binding_is_pending() => _readyWhileBinding.ShouldBeFalse();
    [Fact] void should_not_time_out_into_ready() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_preserve_the_delivery_failure() => _failure.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_not_complete_the_restore() => _log.DidNotReceive().CompleteRestore(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
}
