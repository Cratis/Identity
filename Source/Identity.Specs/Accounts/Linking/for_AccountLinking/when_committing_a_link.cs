// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Identity.Accounts.Linking.Confirming;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.for_AccountLinking;

public class when_committing_a_link : given.an_account_linking_adapter
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_inspect_quarantine_only_after_command_completion(bool clearing)
    {
        _results.Enqueue(new CommandResult<LinkConfirmationOutcome> { Response = new(null) });
        var refused = await Catch.Exception(() => Execute(clearing));
        refused.ShouldBeOfExactType<AccountOperationRefused>();
        refused.Message.ShouldEqual("upstream_key_already_reserved");
        _order.ShouldEqual(["execute"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_fail_closed_without_a_typed_outcome(bool clearing)
    {
        _results.Enqueue(new CommandResult());
        var refused = await Catch.Exception(() => Execute(clearing));
        refused.ShouldBeOfExactType<AccountOperationRefused>();
        refused.Message.ShouldEqual("temporarily_unavailable");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_retry_a_kernel_conflict_with_the_same_observations(bool clearing)
    {
        _results.Enqueue(new CommandResult { ValidationResults = [ValidationResult.Error("ConstraintViolation") with { Reason = ValidationResultReason.ConstraintViolation, ReasonDetail = "unique-upstream-key" }] });
        _results.Enqueue(new CommandResult<LinkConfirmationOutcome> { Response = new(new LinkId(_proposal.Value)) });
        await Execute(clearing);
        _order.ShouldEqual(["execute", "wait", "execute"]);
        _attempts.Count.ShouldEqual(2);
        _attempts[1].Session.ShouldEqual(_attempts[0].Session);
        _attempts[1].Authentication.ShouldEqual(_attempts[0].Authentication);
    }

    Task Execute(bool clearing) => clearing
        ? _adapter.Clear(_ceremony.Session, _account, _proposal, _ceremony.Authentication)
        : _adapter.Confirm(_ceremony.Session, _proposal, _ceremony.Authentication);
}
