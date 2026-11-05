// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>Lookup is a hint; the guarded account stream and kernel constraint authorize writes.</summary>
/// <param name="readModels">The passive projection reader.</param>
public class UpstreamLookups(IReadModels readModels)
{
    /// <summary>Finds a permanent reservation by keyed digest.</summary>
    /// <param name="digest">The lookup pseudonym.</param>
    /// <returns>The reservation, if present.</returns>
    public async Task<UpstreamKeyMatch?> Find(UpstreamLookupDigest digest) => (await readModels.GetInstances<UpstreamKeyLookup>())
        .SelectMany(row => row.Keys.Where(key => key.KeyHash == digest).Select(key => new UpstreamKeyMatch(row.Id, key.Id, key.Active)))
        .SingleOrDefault();
}
