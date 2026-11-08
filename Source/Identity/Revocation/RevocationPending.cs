// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>No acknowledgement is possible; any committed restriction remains irreversible.</summary>
internal sealed class RevocationPending() : Exception("The grant restriction could not be durably confirmed.");
