// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Linking;

/// <summary>The trusted link ceremony's intent and connection policy, not a grant.</summary>
/// <param name="Explicit">Whether linking was explicitly requested.</param>
/// <param name="EmailOnlyProof">Whether only an email hint was supplied, which is insufficient.</param>
/// <param name="SoftDisabledConnection">Whether resolver clearance is required.</param>
internal record LinkIntent(bool Explicit, bool EmailOnlyProof = false, bool SoftDisabledConnection = false);
