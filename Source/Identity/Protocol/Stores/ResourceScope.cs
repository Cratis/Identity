// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>A scope read from the configured resource catalog.</summary>
internal sealed class ResourceScope
{
    /// <summary>Initializes a scope snapshot.</summary>
    /// <param name="descriptor">The declared scope descriptor.</param>
    internal ResourceScope(OpenIddictScopeDescriptor descriptor)
    {
        Descriptor = descriptor;
        Name = descriptor.Name!;
    }

    /// <summary>Gets the declared scope name, also used as its identifier.</summary>
    public string Name { get; }

    /// <summary>Gets the private scope descriptor.</summary>
    internal OpenIddictScopeDescriptor Descriptor { get; }
}
