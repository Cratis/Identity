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
/// <param name="GitHubId">The immutable numeric GitHub id.</param>
/// <param name="GitHubLogin">The mutable GitHub login.</param>
/// <param name="Status">The lifecycle state.</param>
/// <param name="LinkedUpstreamKeys">The active raw keys inside the account erasure boundary.</param>
[ReadModel]
[method: JsonConstructor]
public record Account(AccountSubject Id, PersonalName Name, Email? Email, UpstreamSubject GitHubId, Login GitHubLogin, AccountStatus Status, IEnumerable<LinkedUpstreamKey> LinkedUpstreamKeys)
{
    readonly AccountStatus _status = Status;

    /// <summary>Creates the legacy store's compatibility view during the opt-in split.</summary>
    /// <param name="id">The existing canonical subject.</param>
    /// <param name="gitHubId">The numeric GitHub id.</param>
    /// <param name="gitHubLogin">The display login.</param>
    /// <param name="name">The profile name.</param>
    public Account(string id, long gitHubId, string gitHubLogin, string name)
        : this(new AccountSubject(id), name, null, gitHubId.ToString(System.Globalization.CultureInfo.InvariantCulture), gitHubLogin, AccountStatus.Active, [])
    {
    }

    /// <summary>Gets whether the account has ever been permanently tombstoned.</summary>
    public bool IsTombstoned { get; init; }

    /// <summary>Gets the lifecycle state; enablement cannot resurrect a tombstone.</summary>
    public AccountStatus Status
    {
        get => IsTombstoned ? AccountStatus.Tombstoned : _status;
        init => _status = value;
    }

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
}
