// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Realm;

/// <summary>Identity facts cannot be read or written in a product tenant namespace.</summary>
internal sealed class IdentityRealmNamespaceMismatch() : Exception("identity_realm_namespace_mismatch");
