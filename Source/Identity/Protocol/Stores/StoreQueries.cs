// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>Implements the common in-memory query operations.</summary>
internal static class StoreQueries
{
    /// <summary>Creates a rejection that cannot disclose the attempted write.</summary>
    /// <returns>A read-only rejection.</returns>
    internal static NotSupportedException ReadOnly() => new("The configuration-backed protocol stores are read-only.");

    /// <summary>Returns a read result unless the operation was canceled.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="value">The result.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The read result.</returns>
    internal static ValueTask<T> Read<T>(T value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(value);
    }

    /// <summary>Applies store paging with nonnegative bounds.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The values to page.</param>
    /// <param name="count">The maximum result count.</param>
    /// <param name="offset">The number of values to skip.</param>
    /// <returns>The requested page.</returns>
    internal static IEnumerable<T> Page<T>(IEnumerable<T> values, int? count, int? offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count ?? 0);
        ArgumentOutOfRangeException.ThrowIfNegative(offset ?? 0);
        var page = values.Skip(offset ?? 0);
        return count.HasValue ? page.Take(count.Value) : page;
    }

    /// <summary>Exposes in-memory results as a cancellation-aware asynchronous stream.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The values to enumerate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The asynchronous result stream.</returns>
    internal static async IAsyncEnumerable<T> Enumerate<T>(IEnumerable<T> values, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return value;
        }
    }
}
