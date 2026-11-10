// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventSchemaCompatibility;

public class when_comparing_framework_type_identities : Specification
{
    public enum AssemblyIdentity
    {
        SystemCollections,
        MicrosoftExtensionsOptions,
        CratisIdentity,
        OtherLibrary
    }

    [Fact]
    public void should_accept_core_runtime_version_differences_on_either_side()
    {
        var reflected = Field(typeof(IEnumerable<string>).FullName);
        var retained = reflected with { Type = FrameworkVersion(reflected.Type, new Version(10, 0, 0, 0)) };
        var next = reflected with { Type = FrameworkVersion(reflected.Type, new Version(11, 0, 0, 0)) };
        Compare(retained, next).ShouldBeEmpty();
        Compare(next, retained).ShouldBeEmpty();
        SchemaField.Equivalent([retained], [next]).ShouldBeTrue();
        SchemaField.Equivalent([next], [retained]).ShouldBeTrue();
    }

    [Fact]
    public void should_preserve_nested_generic_structure()
    {
        var identity = typeof(Dictionary<string, IEnumerable<int>>).FullName;
        FrameworkTypeIdentity.Canonicalize(FrameworkVersion(identity, new Version(10, 0, 0, 0)))
            .ShouldEqual(FrameworkTypeIdentity.Canonicalize(FrameworkVersion(identity, new Version(11, 0, 0, 0))));
    }

    [Fact]
    public void should_reject_a_real_element_type_change() =>
        ShouldReject(Field(typeof(IEnumerable<string>).FullName), Field(typeof(IEnumerable<int>).FullName));

    [Fact]
    public void should_reject_an_assembly_name_change()
    {
        var original = Field(typeof(IEnumerable<string>).FullName);
        ShouldReject(original, original with { Type = original.Type.Replace("System.Private.CoreLib", "System.OtherCoreLib", StringComparison.Ordinal) });
    }

    [Fact]
    public void should_reject_a_nullability_change()
    {
        var original = Field(typeof(IEnumerable<string>).FullName);
        ShouldReject(original, original with { Nullable = true });
    }

    [Theory]
    [InlineData(AssemblyIdentity.SystemCollections)]
    [InlineData(AssemblyIdentity.MicrosoftExtensionsOptions)]
    public void should_ignore_only_the_version_for_framework_assemblies(AssemblyIdentity assemblyIdentity)
    {
        var assembly = AssemblyName(assemblyIdentity);
        var identity = $"Example.Type, {assembly}, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
        FrameworkTypeIdentity.Canonicalize(identity).ShouldEqual($"Example.Type, {assembly}, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
    }

    [Theory]
    [InlineData("PublicKeyToken=null")]
    [InlineData("PublicKeyToken=0123456789abcdef")]
    public void should_retain_versions_of_framework_named_assemblies_without_a_microsoft_token(string token)
    {
        var original = Field($"Example.Type, System.Foo, Version=10.0.0.0, Culture=neutral, {token}");
        FrameworkTypeIdentity.Canonicalize(original.Type).ShouldEqual(original.Type);
        ShouldReject(original, original with { Type = original.Type.Replace("10.0.0.0", "11.0.0.0", StringComparison.Ordinal) });
    }

    [Theory]
    [InlineData(AssemblyIdentity.CratisIdentity)]
    [InlineData(AssemblyIdentity.OtherLibrary)]
    public void should_retain_non_framework_versions(AssemblyIdentity assemblyIdentity)
    {
        var assembly = AssemblyName(assemblyIdentity);
        var original = Field($"Example.Type, {assembly}, Version=10.0.0.0, Culture=neutral, PublicKeyToken=null");
        FrameworkTypeIdentity.Canonicalize(original.Type).ShouldEqual(original.Type);
        ShouldReject(original, original with { Type = original.Type.Replace("10.0.0.0", "11.0.0.0", StringComparison.Ordinal) });
    }

    [Theory]
    [InlineData("Culture=neutral", "Culture=en-US")]
    [InlineData("PublicKeyToken=null", "PublicKeyToken=0123456789abcdef")]
    public void should_retain_other_assembly_identity_components(string before, string after)
    {
        var original = Field("Example.Type, System.Example, Version=10.0.0.0, Culture=neutral, PublicKeyToken=null");
        ShouldReject(original, original with { Type = original.Type.Replace(before, after, StringComparison.Ordinal) });
    }

    [Fact]
    public void should_canonicalize_underlying_type_metadata()
    {
        var original = Field("Example.Concept") with { UnderlyingType = typeof(IEnumerable<string>).FullName };
        var next = original with { UnderlyingType = FrameworkVersion(original.UnderlyingType, new Version(11, 0, 0, 0)) };
        Compare(original, next).ShouldBeEmpty();
        SchemaField.Equivalent([original], [next]).ShouldBeTrue();
    }

    static string AssemblyName(AssemblyIdentity identity) => identity switch
    {
        AssemblyIdentity.SystemCollections => "System.Collections",
        AssemblyIdentity.MicrosoftExtensionsOptions => "Microsoft.Extensions.Options",
        AssemblyIdentity.CratisIdentity => "Cratis.Identity",
        AssemblyIdentity.OtherLibrary => "Other.Library",
        _ => throw new ArgumentOutOfRangeException(nameof(identity))
    };

    static SchemaField Field(string type) => new("Values", "values", type, false, false, null, []);

    static string FrameworkVersion(string identity, Version version) =>
        identity.Replace($", Version={typeof(object).Assembly.GetName().Version}", $", Version={version}", StringComparison.Ordinal);

    static IReadOnlyList<Violation> Compare(SchemaField retained, SchemaField reflected)
    {
        var schema = SchemaCompatibility.Describe(typeof(AccountRegistered), ReflectionShapeBuilder.Build(typeof(AccountRegistered)));
        return SchemaCompatibility.Check([schema with { Fields = [reflected] }], [schema with { Fields = [retained] }], _ => true);
    }

    static void ShouldReject(SchemaField retained, SchemaField reflected)
    {
        Compare(retained, reflected).ShouldContain(new Violation("AccountRegistered/1.Values", "event_field_changed_without_generation"));
        SchemaField.Equivalent([retained], [reflected]).ShouldBeFalse();
    }
}
