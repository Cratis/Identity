// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>The configured lookup key does not match the durable startup metadata.</summary>
internal sealed class LookupKeyRevisionMismatch() : Exception("lookup_key_revision_mismatch");
