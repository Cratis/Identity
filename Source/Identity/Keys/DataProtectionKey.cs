// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Identity.Keys;

/// <summary>
/// Represents one Data Protection key provisioned in the secret store.
/// </summary>
/// <param name="Name">The name from the file name.</param>
/// <param name="Element">The key's XML element, as the Data Protection key manager reads it.</param>
/// <param name="ActivationDate">When the key may start protecting payloads.</param>
/// <param name="ExpirationDate">When the key stops being used to protect payloads.</param>
public record DataProtectionKey(string Name, XElement Element, DateTimeOffset ActivationDate, DateTimeOffset ExpirationDate);
