// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventContentRules;

public class when_resolving_default_event_type_ids : Specification
{
    [Fact]
    public void should_use_the_short_type_name_for_a_plain_event_type() => ReflectionShapeBuilder.Build(typeof(AccountRegistered)).TypeId.ShouldEqual("AccountRegistered");

    [Fact]
    public void should_not_include_the_namespace_for_a_dynamic_event() => ReflectionShapeBuilder.ResolveTypeId(Dynamic("Cratis.Left", "Sample")).ShouldEqual("Sample");

    [Fact]
    public void should_resolve_a_prior_generation_to_the_name_of_its_current_type() => ReflectionShapeBuilder.Build(typeof(Previous)).TypeId.ShouldEqual("Current");

    [Fact]
    public void should_resolve_an_explicit_id_and_mark_it_explicit()
    {
        var shape = ReflectionShapeBuilder.Build(typeof(Pinned));
        shape.TypeId.ShouldEqual("pinned-id");
        shape.ExplicitId.ShouldBeTrue();
    }

    [Fact]
    public void should_not_mark_a_default_id_explicit() => ReflectionShapeBuilder.Build(typeof(AccountRegistered)).ExplicitId.ShouldBeFalse();

    [Fact]
    public void should_accept_a_name_that_is_not_a_uuid() => Check(Shape()).ShouldBeEmpty();

    [Fact]
    public void should_reject_two_current_types_with_the_same_name() =>
        CatalogueChecker.CheckNames(Duplicates()).ShouldContain(new Violation("$", "duplicate_event_type_name"));

    [Fact]
    public void should_not_treat_a_prior_generation_as_a_duplicate_name() => CatalogueChecker.CheckNames([typeof(Current), typeof(Previous)]).ShouldBeEmpty();

    [Fact]
    public void should_reject_an_explicit_id_without_a_catalogue_exception() =>
        Check(Shape() with { TypeId = "AccountRegistered", ExplicitId = true }).ShouldContain(new Violation("$", "explicit_event_type_id_not_allowed"));

    [Fact]
    public void should_reject_an_exception_without_a_reason() =>
        CatalogueChecker.Check(Shape() with { TypeId = "kept-id", ExplicitId = true }, Row() with { TypeId = "kept-id", RetainedExplicitId = true })
            .ShouldContain(new Violation("$", "explicit_event_type_id_not_allowed"));

    [Fact]
    public void should_allow_an_explicit_id_with_a_documented_exception() =>
        CatalogueChecker.Check(Shape() with { TypeId = "kept-id", ExplicitId = true }, Row() with { TypeId = "kept-id", RetainedExplicitId = true, Reason = "Renamed after persisting." })
            .ShouldBeEmpty();

    [Fact]
    public void should_require_the_explicit_id_to_equal_the_catalogue_id() =>
        CatalogueChecker.Check(Shape() with { TypeId = "other-id", ExplicitId = true }, Row() with { TypeId = "kept-id", RetainedExplicitId = true, Reason = "Renamed after persisting." })
            .ShouldContain(new Violation("$", "event_type_id_mismatch"));

    [Fact]
    public void should_compare_ids_case_sensitively() => Check(Shape() with { TypeId = "accountregistered" }).ShouldContain(new Violation("$", "event_type_id_mismatch"));

    [Fact]
    public void should_reject_an_empty_id() => Check(Shape() with { TypeId = string.Empty }).ShouldContain(new Violation("$", "event_type_id_mismatch"));

    static EventShape Shape() => ReflectionShapeBuilder.Build(typeof(AccountRegistered));
    static CatalogueRow Row() => CatalogueChecker.Rows.Single(row => row.Name == "AccountRegistered");
    static IReadOnlyList<Violation> Check(EventShape shape) => CatalogueChecker.Check(shape, Row());

    [EventType]
    internal sealed record Current(string Value);
    [EventTypeGenerationFor<Current>(2)]
    internal sealed record Previous(string Value);
    [EventType("pinned-id")]
    internal sealed record Pinned(string Value);

    /// <summary>Builds the pair dynamically so Chronicle's own discovery of this spec assembly never sees two event types with one name.</summary>
    /// <returns>Two distinct event types with the same short name.</returns>
    static Type[] Duplicates() => [Dynamic("Cratis.Left", "Duplicate"), Dynamic("Cratis.Right", "Duplicate")];

    static Type Dynamic(string @namespace, string name)
    {
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run).DefineDynamicModule("Main");
        var type = module.DefineType($"{@namespace}.{name}", TypeAttributes.Public | TypeAttributes.Sealed);
        var constructor = typeof(EventTypeAttribute).GetConstructors().First(candidate => candidate.GetParameters().All(parameter => parameter.IsOptional));
        type.SetCustomAttribute(new CustomAttributeBuilder(constructor, constructor.GetParameters().Select(parameter => parameter.DefaultValue).ToArray()));
        return type.CreateType();
    }
}
