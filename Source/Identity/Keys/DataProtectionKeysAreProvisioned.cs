// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Keys;

/// <summary>
/// The exception that is thrown when the Data Protection key manager tries to store a key. The ring is provisioned
/// by the operator through the secret store; Identity never generates or writes keys.
/// </summary>
public class DataProtectionKeysAreProvisioned()
    : InvalidOperationException("The Data Protection key ring is provisioned through the secret store (data-protection.<name>.xml); Identity does not generate keys.");
