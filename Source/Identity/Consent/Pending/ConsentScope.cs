// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Pending;

/// <summary>An escaped requested scope name and description.</summary>
/// <param name="Name">The escaped scope name.</param>
/// <param name="Description">The escaped catalogue description.</param>
internal record ConsentScope(string Name, string Description);
