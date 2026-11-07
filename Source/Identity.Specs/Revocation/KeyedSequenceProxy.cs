// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Identity.RevocationSpecs;

/// <summary>Captures keyed calls, including default-interface members unsupported by the current Castle proxy.</summary>
public class KeyedSequenceProxy : DispatchProxy
{
    internal bool Supported { get; set; }
    internal AppendResult Result { get; set; } = new();
    internal List<(string Name, object?[] Arguments)> Calls { get; } = [];

    internal static IEventSequence Create(out KeyedSequenceProxy proxy)
    {
        var sequence = Create<IEventSequence, KeyedSequenceProxy>();
        proxy = (KeyedSequenceProxy)sequence;
        return sequence;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod ?? throw new InvalidOperationException();
        Calls.Add((method.Name, args ?? []));
        return method.Name switch
        {
            "get_SupportsIdempotentAppends" => Supported,
            "AppendOnce" => Task.FromResult(Result),
            _ => throw new InvalidOperationException("Unexpected sequence operation.")
        };
    }
}
