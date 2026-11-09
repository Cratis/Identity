// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>Protects a session-bound, expiring decision proof; the Pending CAS enforces single use.</summary>
internal interface IConsentProof
{
    /// <summary>Issues a proof for this exact request and expiry.</summary>
    /// <param name="request">The pending request.</param>
    /// <returns>The protected proof.</returns>
    string Issue(ConsentRequest request);

    /// <summary>Checks protection, request, account, session, realm and expiry.</summary>
    /// <param name="proof">The submitted proof.</param>
    /// <param name="request">The stored request.</param>
    /// <returns>Whether the proof is valid.</returns>
    bool Validate(string? proof, ConsentRequest request);
}
