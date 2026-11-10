// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>The operational authority for a consent request, independent of replayed evidence.</summary>
internal enum ConsentRequestState
{
    /// <summary>No decision has been made.</summary>
    Pending = 0,

    /// <summary>Approval is durable; the evidence outcome may still be unknown.</summary>
    Decided = 1,

    /// <summary>The evidence writer has confirmed the complete append.</summary>
    EvidenceRecorded = 2,

    /// <summary>The operational grant is available, subject to live fence checks.</summary>
    Active = 3,

    /// <summary>The browser denied the request; no evidence was submitted.</summary>
    Denied = 4,

    /// <summary>Resolution confirmed no evidence, and permanently sealed further appends.</summary>
    Abandoned = 5,

    /// <summary>Evidence exists but activation is forbidden; withdrawal is deferred to the #99 contract.</summary>
    EvidenceWithheld = 6
}
