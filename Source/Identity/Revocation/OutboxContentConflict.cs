// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>A persisted delivery identity conflicts with already-committed Chronicle content.</summary>
internal sealed class OutboxContentConflict() : Exception("The revocation outbox contains conflicting committed content.");
