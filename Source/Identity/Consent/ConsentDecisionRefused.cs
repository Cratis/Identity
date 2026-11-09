// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>A uniform refusal that never discloses request existence or personal proof material.</summary>
internal sealed class ConsentDecisionRefused() : Exception("consent_decision_refused");
