// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_the_store_becomes_invalid_on_reload : given.a_secret_store_directory
{
    int _fired;
    string _signerBefore;
    List<string> _messages;

    void Establish()
    {
        _messages = [];
        Open();
        _keys.Dispose();
        _keys = new SecretStoreKeys(_options, _time, new CapturingLogger(_messages));
        _signerBefore = _keys.Current.Signer.Kid;
        _keys.ChangeToken.RegisterChangeCallback(_ => _fired++, null);
        File.WriteAllText(Path.Combine(_directory, "signing.a.key"), "-----BEGIN PRIVATE KEY-----\nSECRETBYTES\n-----END PRIVATE KEY-----");
    }

    void Because() => _keys.Reload();

    [Fact] void should_keep_the_last_valid_keys() => _keys.Current.Signer.Kid.ShouldEqual(_signerBefore);
    [Fact] void should_not_fire_the_change_token() => _fired.ShouldEqual(0);
    [Fact] void should_log_the_file() => _messages.Single().ShouldContain("signing.a.key");
    [Fact] void should_never_log_key_bytes() => _messages.Single().ShouldNotContain("SECRETBYTES");

    class CapturingLogger(List<string> messages) : ILogger<SecretStoreKeys>
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Add(formatter(state, exception));

        sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
