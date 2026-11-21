// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json.Serialization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Accounts;

/// <summary>The account profile owned by its immutable canonical subject.</summary>
/// <param name="Id">The canonical account subject.</param>
/// <param name="Name">The mutable profile name.</param>
/// <param name="Email">The optional profile email, never identity proof.</param>
/// <param name="Status">The lifecycle state.</param>
/// <param name="LinkedUpstreamKeys">The active raw keys inside the account erasure boundary.</param>
[ReadModel]
[method: JsonConstructor]
internal record Account(AccountSubject Id, PersonalName Name, Email? Email, AccountStatus Status, IEnumerable<LinkedUpstreamKey> LinkedUpstreamKeys)
{
    readonly AccountStatus _status = Status;

    /// <summary>Gets whether the account has ever been permanently tombstoned.</summary>
    public bool IsTombstoned { get; init; }

    /// <summary>Gets the lifecycle state; enablement cannot resurrect a tombstone.</summary>
    public AccountStatus Status
    {
        get => IsTombstoned ? AccountStatus.Tombstoned : _status;
        init => _status = value;
    }

    /// <summary>Finds the earliest linked key issued by an exact upstream issuer.</summary>
    /// <param name="issuer">The exact upstream issuer.</param>
    /// <returns>The linked key, or <see langword="null"/> when the account has none for the issuer.</returns>
    public LinkedUpstreamKey? LinkFor(string issuer) => Earliest(LinkedUpstreamKeys.Where(_ => string.Equals(_.IdpIssuer.Value, issuer, StringComparison.Ordinal)));

    /// <summary>Gets the display login of the earliest linked key, independent of provider.</summary>
    /// <returns>The login, or <see langword="null"/> when the account has no linked key.</returns>
    public Login? DisplayLogin() => Earliest(LinkedUpstreamKeys)?.Login;

    /// <summary>Reads only the current authenticated account; the owner is never query input.</summary>
    /// <param name="context">The trusted session.</param>
    /// <param name="accounts">The active-account projection reader.</param>
    /// <returns>The caller's account, if present.</returns>
    /// <exception cref="AccountOperationRefused">No authenticated account is available.</exception>
    [Authorize]
    public static async Task<Account?> Mine(IHttpContextAccessor context, IAccountReader accounts)
    {
        var user = context.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new AccountOperationRefused("authenticated_account_required");
        }

        if (!AccountSubject.TryParse(user.FindFirstValue(Claims.Subject) ?? user.FindFirstValue(ClaimTypes.NameIdentifier), out var subject))
        {
            return null;
        }

        return await accounts.FindActive(subject!);
    }

    static LinkedUpstreamKey? Earliest(IEnumerable<LinkedUpstreamKey> keys) => keys
        .OrderBy(_ => _.LinkedAt)
        .ThenBy(_ => _.LinkId.Value)
        .FirstOrDefault();
}
