// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts;

/// <summary>Mutable observations, never matching or authorization evidence.</summary>
/// <param name="Email">The email observation.</param>
/// <param name="Name">The name observation.</param>
/// <param name="Login">The per-key display observation.</param>
internal record ObservedProfile(Email Email, PersonalName Name, Login Login);
