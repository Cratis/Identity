// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>Durable key-check metadata, separate from event payloads and never secret material.</summary>
/// <param name="Id">The one retained revision.</param>
/// <param name="Check">The domain-separated key check.</param>
internal record LookupRevision(int Id, string Check);
