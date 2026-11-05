// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.v3;

namespace Cratis.Identity.Integration.Conformance;

/// <summary>
/// Traces a spec to a protocol requirement and selects it for the conformance step.
/// </summary>
/// <param name="id">The requirement identifier.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirementAttribute(string id) : Attribute, ITraitAttribute
{
    public string Id { get; } = id;

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
    [
        new("Category", "Conformance"),
        new("Requirement", Id)
    ];
}
