// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Tenants;

/// <summary>
/// Represents a tenant an account can select when authorizing a client.
/// </summary>
/// <param name="Id">The tenant identifier written to the <c language="text">tid</c> claim.</param>
/// <param name="Name">The human-readable tenant name.</param>
internal record SelectableTenant(string Id, string Name);
