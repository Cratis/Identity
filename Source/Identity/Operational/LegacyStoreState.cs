// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>What a read-only look at a legacy MongoDB account collection found.</summary>
public enum LegacyStoreState
{
    /// <summary>The collection does not exist.</summary>
    Absent = 0,

    /// <summary>The collection exists and holds no documents.</summary>
    Empty = 1,

    /// <summary>The collection holds at least one document.</summary>
    HasAccounts = 2
}
