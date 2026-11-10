// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Contracts.Check;

/// <summary>The catalogue and complete fixture set at one repository state.</summary>
/// <param name="Catalogue">The persisted generation declarations.</param>
/// <param name="Fixtures">Fixture paths and their exact bytes.</param>
internal sealed record ReleasedSchemaState(IReadOnlyList<EventSchema> Catalogue, IReadOnlyDictionary<string, byte[]> Fixtures);
