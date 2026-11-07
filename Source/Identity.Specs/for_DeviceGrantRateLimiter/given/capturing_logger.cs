// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Device;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.for_DeviceGrantRateLimiter.given;

internal sealed class capturing_logger : ILogger<DeviceGrantRateLimiter>
{
    internal List<string> Messages { get; } = [];
    internal List<string> Values { get; } = [];
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
        if (state is IEnumerable<KeyValuePair<string, object?>> fields)
        {
            Values.AddRange(fields.Where(_ => _.Key != "{OriginalFormat}").Select(_ => _.Value?.ToString() ?? string.Empty));
        }
    }
}
