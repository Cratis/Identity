// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.Integration.for_NativeAccounts.given;

/// <summary>
/// Captures only defined account refusals; unexpected pipeline errors still fail the spec.
/// </summary>
/// <param name="Account">The account available for session construction, if any.</param>
/// <param name="Failure">The defined refusal, if any.</param>
public record SignInOutcome(Account? Account, string? Failure)
{
    /// <summary>
    /// Observes a real adapter call without replacing its command or append results.
    /// </summary>
    /// <param name="signIn">The real adapter call.</param>
    /// <returns>The session account or defined refusal.</returns>
    public static async Task<SignInOutcome> Observe(Task<Account> signIn)
    {
        try
        {
            return new(await signIn.WaitAsync(TimeSpan.FromSeconds(30)), null);
        }
        catch (AccountOperationRefused refusal)
        {
            return new(null, refusal.Message);
        }
    }
}
