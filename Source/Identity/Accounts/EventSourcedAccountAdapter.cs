// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Identity.Accounts.SigningIn;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Protocol;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Bridges the trusted upstream protocol to native commands and caught-up account views.
/// </summary>
/// <param name="commands">The realm-scoped command boundary.</param>
/// <param name="accounts">The passive account reader.</param>
public class EventSourcedAccountAdapter(IProtocolCommands commands, AccountReader accounts) : IAccounts, IAccountSignIn
{
    /// <inheritdoc/>
    /// <exception cref="AccountOperationRefused">A DTO alone is not verified authentication.</exception>
    public Task<Account> RecordGitHubSignIn(GitHubUser user, CancellationToken cancellationToken = default) =>
        throw new AccountOperationRefused("verified_sign_in_required");

    /// <inheritdoc/>
    public Task<Account?> FindById(string id, CancellationToken cancellationToken = default) =>
        AccountSubject.TryParse(id, out var subject) ? accounts.FindActive(subject!) : Task.FromResult<Account?>(null);

    /// <summary>
    /// Executes one guarded decision and at most one fresh retry, never replaying stale events.
    /// </summary>
    /// <param name="authentication">The verified upstream authentication.</param>
    /// <param name="profile">The observed profile.</param>
    /// <returns>The caught-up active account for session construction.</returns>
    /// <exception cref="AccountOperationRefused">The guarded decision or required projection progress was refused.</exception>
    public async Task<Account> SignIn(VerifiedAuthentication authentication, ObservedProfile profile)
    {
        try
        {
            var result = await Execute(authentication, profile);
            if (Retryable(result))
            {
                await commands.BeforeRetry();
                result = await Execute(authentication, profile);
            }

            if (!result.IsSuccess)
            {
                throw new AccountOperationRefused(Failure(result));
            }

            if (result is not CommandResult<AccountSubject> { Response: { } subject })
            {
                throw new AccountOperationRefused("temporarily_unavailable");
            }

            return await accounts.FindActive(subject) ?? throw new AccountOperationRefused("account_not_active");
        }
        catch (ReadModelsNotCaughtUp)
        {
            throw new AccountOperationRefused("temporarily_unavailable");
        }
    }

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

    Task<CommandResult> Execute(VerifiedAuthentication authentication, ObservedProfile profile) => commands.Execute(
        new SignInWithUpstreamKey(authentication.OperationId),
        provider => provider.GetRequiredService<VerifiedUpstreamSignIn>().Set(authentication, profile));
}
