// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Configuration;

/// <summary>Native account lookup and consistency options.</summary>
public class AccountsOptions
{
    /// <summary>Gets or sets the base64 lookup secret, never logged.</summary>
    public string LookupKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the single supported lookup-key revision.</summary>
    public int LookupKeyRevision { get; set; } = 1;

    /// <summary>Gets or sets the positive budget for projection catch-up at the protocol boundary.</summary>
    public TimeSpan ReadModelCatchUpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the minimum explicit-link proof hold.</summary>
    public TimeSpan LinkHoldMinimum { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets whether the operator confirms there is no legacy MongoDB account store to lose. Defaults to false:
    /// startup is refused while a legacy accounts collection has documents or cannot be checked. Set it only in
    /// Development or after confirming the legacy data is obsolete; accounts are never migrated.
    /// </summary>
    public bool ConfirmNoLegacyMongoStore { get; set; }
}
