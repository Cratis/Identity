// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>The complete evidence-append outcome, not merely the presence of one event.</summary>
public enum ConsentEvidenceResolution
{
    /// <summary>The outcome cannot be confirmed; neither activate nor abandon. This is the safe default.</summary>
    Unknown = 0,

    /// <summary>The complete evidence append is durable.</summary>
    Recorded = 1,

    /// <summary>No evidence exists and further appends for this identity are permanently sealed.</summary>
    NotRecorded = 2
}
