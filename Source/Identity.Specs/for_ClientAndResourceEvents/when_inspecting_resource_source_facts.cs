// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Compliance;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;

namespace Cratis.Identity.for_ClientAndResourceEvents;

public class when_inspecting_resource_source_facts : Specification
{
    [Theory]
    [InlineData(typeof(ResourceRegistrationRecorded), "ResourceRegistrationRecorded", "Audience,DisplayName,FactId,ResourceIndicator,ScopePrefix")]
    [InlineData(typeof(ResourceScopeRegistrationRecorded), "ResourceScopeRegistrationRecorded", "Description,FactId,ScopeName")]
    [InlineData(typeof(ResourceScopeRetirementRecorded), "ResourceScopeRetirementRecorded", "FactId,RetiredAt,ScopeName")]
    public void should_pin_the_additive_internal_contracts(Type type, string id, string fields)
    {
        var shape = ReflectionShapeBuilder.Build(type);
        shape.TypeId.ShouldEqual(id);
        shape.Generation.ShouldEqual(1u);
        shape.Visibility.ShouldEqual("Internal");
        string.Join(',', shape.Members.Select(member => member.ClrName).Order(StringComparer.Ordinal)).ShouldEqual(fields);
        type.GetProperty("FactId").PropertyType.ShouldEqual(typeof(FactId));
        ContentRules.Check(shape, SafeConceptRegistry.Entries).ShouldBeEmpty();
        CatalogueChecker.Check(shape, CatalogueChecker.Rows.Single(row => row.Name == type.Name)).ShouldBeEmpty();
    }

    [Fact]
    void should_keep_business_labels_off_the_public_contracts()
    {
        typeof(ResourceRegistered).GetProperty("DisplayName").ShouldBeNull();
        typeof(ResourceScopeRegistered).GetProperty("Description").ShouldBeNull();
        var publicLabel = ReflectionShapeBuilder.Build(typeof(ResourceRegistrationRecorded)) with { Visibility = "Public" };
        ContentRules.Check(publicLabel, SafeConceptRegistry.Entries).ShouldContain(new Violation("DisplayName", "missing_pii"));
    }

    [Theory]
    [InlineData(typeof(RawLabel), "DisplayName", "missing_pii")]
    [InlineData(typeof(MisnamedLabel), "ClientSecret", "unapproved_safe_concept")]
    [InlineData(typeof(ExtendedLabel), "DisplayName.Secret", "forbidden_member_name")]
    public void should_not_allow_resource_labels_to_hide_sensitive_content(Type type, string path, string reason) =>
        ContentRules.Check(ReflectionShapeBuilder.Build(type), SafeConceptRegistry.Entries).ShouldContain(new Violation(path, reason));

    internal sealed record RawLabel(string DisplayName);
    internal sealed record MisnamedLabel(ResourceDisplayName ClientSecret);
    internal sealed record ExtendedLabel(ExtraLabel DisplayName);
    internal sealed record ExtraLabel(string Value, string Secret) : ConceptAs<string>(Value);
}
