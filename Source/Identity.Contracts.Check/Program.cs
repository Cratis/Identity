// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("event-content $ contracts_assembly_required");
    return 1;
}
try
{
    var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
    if (assembly.GetName().Name != typeof(AccountRegistered).Assembly.GetName().Name)
    {
        Console.Error.WriteLine("event-content $ contracts_assembly_mismatch");
        return 1;
    }
    return CheckRunner.Run(ReflectionShapeBuilder.Discover(assembly), Console.Out);
}
catch (Exception)
{
    Console.Error.WriteLine("event-content $ discovery_failed");
    return 1;
}
