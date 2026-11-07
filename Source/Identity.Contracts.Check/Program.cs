// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Contracts.Check;

/// <summary>Checks every production Identity assembly supplied by the build.</summary>
internal static class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2 || args.Any(path => !File.Exists(path)))
        {
            Console.Error.WriteLine("event-content $ contracts_assembly_required");
            return 1;
        }
        try
        {
            var assemblies = args[..^1].Select(path => Assembly.LoadFrom(Path.GetFullPath(path))).ToArray();
            if (!assemblies.Any(assembly => assembly.GetName().Name == typeof(AccountRegistered).Assembly.GetName().Name))
            {
                Console.Error.WriteLine("event-content $ contracts_assembly_mismatch");
                return 1;
            }
            return CheckRunner.Run(assemblies, args[^1], Console.Out);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("event-content $ discovery_failed");
            return 1;
        }
    }
}
