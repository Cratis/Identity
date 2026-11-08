// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts;

/// <summary>A fail-closed account operation, containing only an allowlisted reason.</summary>
/// <param name="reason">The reason code, never evidence or personal content.</param>
internal class AccountOperationRefused(string reason) : Exception(reason)
{
    /// <summary>Requires a policy condition without including personal proof material in errors.</summary>
    /// <param name="satisfied">Whether policy is satisfied.</param>
    /// <param name="reason">The allowlisted refusal code.</param>
    /// <exception cref="AccountOperationRefused">The required condition is not satisfied.</exception>
    internal static void Require(bool satisfied, string reason)
    {
        if (!satisfied)
        {
            throw new AccountOperationRefused(reason);
        }
    }
}
