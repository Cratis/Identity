// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

namespace Cratis.Identity.for_EventContentRules;

public class when_scanning_production_assemblies : Specification
{
    static readonly string _solution = Path.GetFullPath("../../../../../Identity.slnx", AppContext.BaseDirectory);
    static readonly Assembly[] _production = [typeof(AccountRegistered).Assembly, Assembly.Load("Cratis.Identity"), Assembly.Load("Cratis.Identity.Host"), Assembly.Load("Cratis.Id.Host")];

    [Fact]
    public void should_scan_every_production_project()
    {
        using var output = new StringWriter();
        CheckRunner.Run(_production, _solution, output).ShouldEqual(0);
        output.ToString().Contains($"checked {CatalogueChecker.Rows.Count(row => row.TypeId is not null)} event shapes; 0 violations", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void should_fail_when_a_production_assembly_is_not_listed()
    {
        using var output = new StringWriter();
        CheckRunner.Run(_production.Where(assembly => assembly.GetName().Name != "Cratis.Identity.Host"), _solution, output).ShouldEqual(1);
        output.ToString().ShouldEqual($"event-content Cratis.Identity.Host production_assembly_not_scanned{Environment.NewLine}");
    }

    [Fact]
    public void should_fail_when_the_hosted_server_is_not_scanned()
    {
        using var output = new StringWriter();
        CheckRunner.Run(_production.Where(assembly => assembly.GetName().Name != "Cratis.Id.Host"), _solution, output).ShouldEqual(1);
        output.ToString().ShouldEqual($"event-content Cratis.Id.Host production_assembly_not_scanned{Environment.NewLine}");
    }

    [Fact]
    public void should_discover_and_reject_a_violating_registration_in_a_second_assembly()
    {
        var assemblies = _production.Append(typeof(when_discovering_event_artifacts.InternalArtifact).Assembly).ToArray();
        ReflectionShapeBuilder.Discover(assemblies).ShouldContain(typeof(when_discovering_event_artifacts.InternalArtifact));
        using var output = new StringWriter();
        CheckRunner.Run(assemblies, _solution, output).ShouldEqual(1);
        output.ToString().Contains("InternalArtifact", StringComparison.Ordinal).ShouldBeTrue();
        output.ToString().Contains("event_not_catalogued", StringComparison.Ordinal).ShouldBeTrue();
        output.ToString().Contains("forbidden_member_name", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_catalogue_entries_without_a_registration()
    {
        using var output = new StringWriter();
        CheckRunner.Run(ReflectionShapeBuilder.Discover(_production).Where(type => type != typeof(AccountRegistered)), output).ShouldEqual(1);
        output.ToString().Contains("AccountRegistered", StringComparison.Ordinal).ShouldBeTrue();
        output.ToString().Contains("allocated_event_missing", StringComparison.Ordinal).ShouldBeTrue();
    }
}
