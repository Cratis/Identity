// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Independent restriction coverage; future classes remain denied until covered.</summary>
internal enum RestrictionClass
{
    /// <summary>Independently logged grant replay.</summary>
    Grants = 0,

    /// <summary>Immutable creation epochs, including code and access entries.</summary>
    Tokens = 1,

    /// <summary>Immutable authorization creation epochs.</summary>
    Authorizations = 2,

    /// <summary>Immutable request creation epochs.</summary>
    Consent = 3,

    /// <summary>Secret-store keys outside the restore set.</summary>
    Keys = 4,

    /// <summary>Configured client secrets outside the restore set.</summary>
    ClientSecrets = 5,

    /// <summary>Legacy bootstrap, permanently denied following a restore.</summary>
    LegacyBootstrap = 6,

    /// <summary>Account status and upstream links, not yet covered.</summary>
    Accounts = 7,

    /// <summary>Browser sessions, owned by their issuing host.</summary>
    BrowserSessions = 8,

    /// <summary>On-behalf sessions, owned by their issuing host.</summary>
    OnBehalfSessions = 9,

    /// <summary>Runner sessions, owned by their issuing host.</summary>
    RunnerSessions = 10,

    /// <summary>Delegation descendants, budgets and approvals, not yet covered.</summary>
    Delegation = 11,

    /// <summary>Product personal access tokens, owned by their issuing host.</summary>
    PersonalAccessTokens = 12
}
