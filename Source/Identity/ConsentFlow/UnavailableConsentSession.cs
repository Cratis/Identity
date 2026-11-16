// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Fail-closed until the consent flow owns and wires the real session claim and fencing.</summary>
internal sealed class UnavailableConsentSession : ICurrentConsentSession
{
    /// <inheritdoc/>
    public string? SessionId => null;
}
