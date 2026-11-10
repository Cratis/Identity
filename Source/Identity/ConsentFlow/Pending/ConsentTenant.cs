// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Pending;

/// <summary>A selectable tenant identifier and escaped display name.</summary>
/// <param name="Id">The tenant identifier, used only as data.</param>
/// <param name="Name">The escaped tenant name.</param>
internal record ConsentTenant(string Id, string Name);
