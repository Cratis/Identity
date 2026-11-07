// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard.given;

public class a_legacy_store_guard : Specification
{
    protected const string ConfiguredConnection = "mongodb://identity-user:canary-secret@db.internal:27017";
    protected const string DefaultConnection = "mongodb://localhost:27017";
    protected IdentityOptions _options;
    protected FakeProbe _probe;
    protected CapturingLogger _logger;
    protected Exception _error;

    protected string LoggedText => string.Join('\n', _logger.Entries.Select(entry => entry.Message));

    void Establish()
    {
        _options = new();
        _options.MongoDB.ConnectionString = ConfiguredConnection;
        _options.MongoDB.Database = "configured-db";
        _probe = new();
        _logger = new();
    }

    protected async Task Start()
    {
        var guard = new LegacyAccountStoreGuard(Options.Create(_options), _probe, _logger);
        try
        {
            await guard.StartAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is LegacyAccountStoreFound or LegacyAccountStoreUnverifiable)
        {
            _error = exception;
        }
    }

    protected void NothingConfigured() => _options.MongoDB = new();

    public sealed class FakeProbe : ILegacyAccountStoreProbe
    {
        readonly Dictionary<string, object> _results = [];

        public List<(string ConnectionString, string Database)> Calls { get; } = [];

        public void Returns(string connectionString, LegacyStoreState state) => _results[connectionString] = state;

        public void Throws(string connectionString, Exception exception) => _results[connectionString] = exception;

        public Task<LegacyStoreState> Check(string connectionString, string database, CancellationToken cancellationToken)
        {
            Calls.Add((connectionString, database));
            return _results.GetValueOrDefault(connectionString) switch
            {
                Exception exception => Task.FromException<LegacyStoreState>(exception),
                LegacyStoreState state => Task.FromResult(state),
                _ => Task.FromResult(LegacyStoreState.Absent)
            };
        }
    }

    public sealed class CapturingLogger : ILogger<LegacyAccountStoreGuard>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception) + exception));
    }
}
