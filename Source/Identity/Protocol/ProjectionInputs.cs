// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Protocol;

/// <summary>Projection inputs mirrored from Define; architecture specs prevent drift.</summary>
internal static class ProjectionInputs
{
    /// <summary>Gets the read models required by upstream-key sign-in.</summary>
    internal static IReadOnlyList<Type> SignInModels { get; } = [typeof(Account), typeof(UpstreamKeyLookup)];

    /// <summary>Gets the exact input event types for a realm projection.</summary>
    /// <param name="readModel">The projection's model.</param>
    /// <returns>Its nonempty event-type filter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The model has no explicit filter.</exception>
    internal static IReadOnlyList<Type> For(Type readModel) => readModel == typeof(UpstreamKeyLookup)
        ? [typeof(UpstreamKeyLinkRecorded), typeof(UpstreamKeyUnlinkRecorded)]
        : readModel == typeof(Account)
            ? [typeof(AccountRegistrationRecorded), typeof(AccountNameChanged), typeof(AccountEmailChanged),
                typeof(AccountDisablementRecorded), typeof(AccountEnablementRecorded), typeof(AccountTombstoneRecorded),
                typeof(UpstreamKeyLinkRecorded), typeof(UpstreamKeySubjectRecorded), typeof(UpstreamKeyDisplayIdentifierObserved), typeof(UpstreamKeyUnlinkRecorded)]
            : throw new ArgumentOutOfRangeException(nameof(readModel));
}
