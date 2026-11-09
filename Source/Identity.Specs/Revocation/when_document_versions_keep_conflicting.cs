// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs;

public class when_document_versions_keep_conflicting : for_RedeemRefreshTokenOnce.given.a_refresh_token_request
{
    void Establish()
    {
        _tokens.HasStatusAsync(_token, Statuses.Valid, Arg.Any<CancellationToken>()).Returns(true);
        _fences.RedeemAndInstall(AuthorizationId, TokenId, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(RedeemOutcome.VersionConflict);
    }
    async Task Because() => await _handler.HandleAsync(_context);
    [Fact] void should_bound_retries_instead_of_spinning() => _fences.Received(3).RedeemAndInstall(AuthorizationId, TokenId, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    [Fact] void should_return_a_temporary_denial() => _context.Error.ShouldEqual(Errors.TemporarilyUnavailable);
    [Fact] void should_not_consume_an_entry_without_admission() => _tokens.DidNotReceive().TryRedeemAsync(_token, Arg.Any<CancellationToken>());
    [Fact] void should_not_report_version_churn_as_compromise() => _fences.DidNotReceive().Commit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>());
}
