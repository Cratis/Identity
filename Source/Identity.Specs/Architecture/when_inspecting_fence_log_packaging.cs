// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Accounts;
using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;

namespace Cratis.Identity.Architecture;

public class when_inspecting_fence_log_packaging : Specification
{
    const string RemovedImplementation = "CoLocatedMongoFenceLog";

    static readonly string[] _forbiddenContractReferences = ["MongoDB", "OpenIddict", "Microsoft.AspNetCore"];

    Assembly[] _assemblies;
    Type[] _coLocated;
    string[] _contractReferences;
    string[] _filesReferencingRemovedImplementation;
    int _scannedFiles;
    Type[] _publicContract;

    void Because()
    {
        _assemblies = [typeof(Account).Assembly, typeof(FenceRecord).Assembly, typeof(MongoIndependentFenceLog).Assembly];
        _coLocated = _assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.Contains("CoLocated", StringComparison.OrdinalIgnoreCase) && type.Name.Contains("FenceLog", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        _contractReferences = typeof(FenceRecord).Assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty)
            .Where(name => _forbiddenContractReferences.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToArray();
        _publicContract = typeof(FenceRecord).Assembly.GetExportedTypes();

        var root = RepositoryRoot();
        var files = new[] { "Source", "Documentation" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories))
            .Where(file => Path.GetFileName(file) != $"{nameof(when_inspecting_fence_log_packaging)}.cs" &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !file.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}") &&
                           new[] { ".cs", ".csproj", ".md", ".json", ".slnx", ".props" }.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();
        _scannedFiles = files.Length;
        _filesReferencingRemovedImplementation = files.Where(file => File.ReadAllText(file).Contains(RemovedImplementation, StringComparison.Ordinal)).ToArray();
    }

    [Fact] void should_inspect_the_adapter_and_contract_assemblies() => _assemblies.Select(assembly => assembly.GetName().Name).Distinct().Count().ShouldEqual(3);
    [Fact] void should_have_no_co_located_fence_log_type_in_any_identity_assembly() => _coLocated.ShouldBeEmpty();
    [Fact] void should_have_scanned_source_and_documentation() => (_scannedFiles > 100).ShouldBeTrue();
    [Fact] void should_have_no_file_referencing_the_removed_implementation() => _filesReferencingRemovedImplementation.ShouldBeEmpty();
    [Fact] void should_keep_storage_and_web_stack_out_of_the_contract_package() => _contractReferences.ShouldBeEmpty();
    [Fact] void should_publish_the_fence_log_ports() => new[] { typeof(IIndependentFenceLog), typeof(IReservationLog), typeof(IRecoveryLog) }.All(port => _publicContract.Contains(port)).ShouldBeTrue();
    [Fact] void should_name_the_published_assemblies_after_their_packages() => new[] { typeof(FenceRecord).Assembly.GetName().Name, typeof(MongoIndependentFenceLog).Assembly.GetName().Name }.ShouldEqual(["Cratis.Identity.FenceLog", "Cratis.Identity.FenceLog.Mongo"]);

    static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Identity.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Identity.slnx was not found above the specification output.");
    }
}
