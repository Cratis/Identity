// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>A mechanically transcribed catalogue row.</summary>
/// <param name="Name">The event name.</param>
/// <param name="TypeId">The allocated identity or null for a placeholder.</param>
/// <param name="Area">The namespace area.</param>
/// <param name="Visibility">The publication status.</param>
/// <param name="Subject">The stream subject alias.</param>
/// <param name="Fields">The exact field list with protection marks.</param>
/// <param name="Assembly">The production assembly and namespace root.</param>
/// <param name="RetainedExplicitId">Whether the event deliberately keeps an explicit attribute id.</param>
/// <param name="Reason">The documented reason for a retained explicit id.</param>
internal sealed record CatalogueRow(string Name, string? TypeId, string Area, string Visibility, string Subject, string Fields, string Assembly = "Cratis.Identity.Contracts", bool RetainedExplicitId = false, string? Reason = null);
