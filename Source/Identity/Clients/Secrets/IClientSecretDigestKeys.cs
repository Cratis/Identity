// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.Secrets;

/// <summary>The custody boundary for the current client-secret digest key.</summary>
public interface IClientSecretDigestKeys
{
    /// <summary>Gets the current key.</summary>
    ClientSecretDigestKey Current { get; }
}
