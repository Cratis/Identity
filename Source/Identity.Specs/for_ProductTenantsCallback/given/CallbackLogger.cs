// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Identity.for_ProductTenantsCallback.given;

public class CallbackLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];
    public List<Exception> Exceptions { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
        if (exception is not null)
        {
            Exceptions.Add(exception);
        }
    }
}
