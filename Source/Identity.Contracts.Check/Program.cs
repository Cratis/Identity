// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Checks every production Identity assembly supplied by the build.</summary>
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.FirstOrDefault() == "--released-ref") return await RunReleasedRef(args);
        if (args.Length < 2 || args.Any(path => !File.Exists(path)))
        {
            await Console.Error.WriteLineAsync("event-content $ contracts_assembly_required");
            return 1;
        }
        try
        {
            var assemblies = args[..^1].Select(path => Assembly.LoadFrom(Path.GetFullPath(path))).ToArray();
            if (!assemblies.Any(assembly => assembly.GetName().Name == typeof(AccountRegistered).Assembly.GetName().Name))
            {
                await Console.Error.WriteLineAsync("event-content $ contracts_assembly_mismatch");
                return 1;
            }
            return CheckRunner.Run(assemblies, args[^1], Console.Out);
        }
        catch (Exception)
        {
            await Console.Error.WriteLineAsync("event-content $ discovery_failed");
            return 1;
        }
    }

    static async Task<int> RunReleasedRef(string[] args)
    {
        if (!ValidReleasedRefArguments(args))
        {
            await Console.Error.WriteLineAsync("event-schema $ released_ref_and_solution_required");
            return 1;
        }
        return await ReleasedSchemaCheck.Run(args[1], args[2], Console.Out);
    }

    static bool ValidReleasedRefArguments(string[] args) => args.Length == 3 && !string.IsNullOrWhiteSpace(args[1]) && File.Exists(args[2]);
}
