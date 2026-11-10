// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Identity.Accounts.Linking.Proposing;
using Cratis.Identity.Accounts.Linking.Rejecting;
using Cratis.Identity.Accounts.Linking.Unlinking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>
/// Bridges trusted link ceremonies to native protocol-only commands without appending events.
/// </summary>
/// <param name="commands">The guarded protocol command pipeline.</param>
internal sealed class AccountLinking(IProtocolCommands commands)
{
    /// <summary>
    /// Proposes a link using the existing session and adapter-verified upstream proof.
    /// </summary>
    /// <param name="session">The existing authenticated account principal.</param>
    /// <param name="authentication">The adapter-verified upstream proof.</param>
    /// <param name="intent">The explicit ceremony intent and trusted connection policy.</param>
    /// <param name="cancellationToken">The originating request's cancellation.</param>
    /// <returns>The proposal identifier.</returns>
    /// <exception cref="AccountOperationRefused">The ceremony or command pipeline refuses the proposal.</exception>
    public async Task<LinkProposalId> Propose(ClaimsPrincipal session, VerifiedAuthentication authentication, LinkIntent intent, CancellationToken cancellationToken = default)
    {
        var result = await Execute(new ProposeLink(), session, authentication, intent, cancellationToken);
        if (result is not CommandResult<LinkProposalId> { Response: { } proposal })
        {
            throw new AccountOperationRefused("temporarily_unavailable");
        }

        return proposal;
    }

    /// <summary>
    /// Terminally rejects an account owner's proposal through the command pipeline.
    /// </summary>
    /// <param name="session">The owning account principal.</param>
    /// <param name="proposalId">The proposal identifier.</param>
    /// <param name="cancellationToken">The originating request's cancellation.</param>
    /// <returns>The command completion.</returns>
    /// <exception cref="AccountOperationRefused">The session, account, proposal, or pipeline refuses rejection.</exception>
    public async Task Reject(ClaimsPrincipal session, LinkProposalId proposalId, CancellationToken cancellationToken = default) =>
        _ = await Execute(new RejectLink(proposalId), session, null, new(false), cancellationToken);

    /// <summary>
    /// Removes a confirmed key while preserving its permanent reservation.
    /// </summary>
    /// <param name="session">The owning account principal.</param>
    /// <param name="linkId">The confirmed link identifier.</param>
    /// <param name="cancellationToken">The originating request's cancellation.</param>
    /// <returns>The command completion.</returns>
    /// <exception cref="AccountOperationRefused">The session, account, key, or pipeline refuses removal.</exception>
    public async Task Unlink(ClaimsPrincipal session, LinkId linkId, CancellationToken cancellationToken = default) =>
        _ = await Execute(new UnlinkUpstreamKey(linkId), session, null, new(false), cancellationToken);

    static bool Retryable(CommandResult result) => result.ValidationResults.Any(validation =>
        validation.Reason == ValidationResultReason.ConcurrencyViolation ||
        (validation.Reason == ValidationResultReason.ConstraintViolation && validation.ReasonDetail == "unique-upstream-key") ||
        validation.Message == "subject_already_reserved");

    static string Failure(CommandResult result)
    {
        if (result.HasExceptions)
        {
            return "temporarily_unavailable";
        }

        if (result.ValidationResults.Any(validation => validation.Reason == ValidationResultReason.ConcurrencyViolation))
        {
            return "account_concurrency_conflict";
        }

        if (result.ValidationResults.Any(validation => validation.Reason == ValidationResultReason.ConstraintViolation))
        {
            return "upstream_key_already_reserved";
        }

        return result.ValidationResults.FirstOrDefault()?.Message ?? "temporarily_unavailable";
    }

    async Task<CommandResult> Execute(object command, ClaimsPrincipal session, VerifiedAuthentication? authentication, LinkIntent intent, CancellationToken cancellationToken)
    {
        try
        {
            var result = await Attempt(command, session, authentication, intent, cancellationToken);
            if (Retryable(result))
            {
                await commands.BeforeRetry(cancellationToken);
                result = await Attempt(command, session, authentication, intent, cancellationToken);
            }

            if (!result.IsSuccess)
            {
                throw new AccountOperationRefused(Failure(result));
            }

            return result;
        }
        catch (ReadModelsNotCaughtUp)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new AccountOperationRefused("temporarily_unavailable");
        }
    }

    async Task<CommandResult> Attempt(object command, ClaimsPrincipal session, VerifiedAuthentication? authentication, LinkIntent intent, CancellationToken cancellationToken)
    {
        var result = await commands.Execute(command, services => services.GetRequiredService<LinkCeremony>().Set(session, authentication, intent, cancellationToken), cancellationToken);

        // Arc captures cancellation as a failure; a gone caller must never become a refusal.
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
