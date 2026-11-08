// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Pending;

/// <summary>A raw requested scope name and escaped description.</summary>
/// <param name="Name">The raw scope name; hosts must render it as text, not HTML.</param>
/// <param name="Description">The escaped catalogue description.</param>
internal record ConsentScope(string Name, string Description);
