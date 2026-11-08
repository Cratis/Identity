// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>The single-revision lookup-key boundary; production secret storage belongs to #38.</summary>
internal interface IUpstreamLookupKeys
{
    /// <summary>Gets the current key.</summary>
    UpstreamLookupKey Current { get; }
}
