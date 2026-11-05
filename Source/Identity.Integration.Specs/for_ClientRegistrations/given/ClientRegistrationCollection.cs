// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_ClientRegistrations.given;

/// <summary>Isolates real-kernel client reconciliation from legacy Mongo-only fixtures.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class ClientRegistrationCollection
{
    public const string Name = "Chronicle client registrations";
}
