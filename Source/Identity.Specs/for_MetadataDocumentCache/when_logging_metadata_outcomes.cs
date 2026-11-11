// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.for_MetadataDocumentCache;

public class when_logging_metadata_outcomes : Specification
{
    [Fact]
    async Task should_log_only_the_host_reason_duration_and_hit_not_the_path_or_self_asserted_name()
    {
        const string Id = "https://client.example/untrusted-path.json";
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(new MetadataDocumentResult(
            MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(Id), "synthetic-untrusted-name", [new Uri("https://client.example/callback")])),
            MetadataRefusal.None));
        var logger = new RecordingLogger();
        var resolver = new ClientIdMetadataDocuments(fetcher, new() { Enabled = true }, Substitute.For<IResourceCatalog>(), TimeProvider.System, logger);
        await resolver.Resolve(new ClientId(Id), CancellationToken.None);
        await resolver.Resolve(new ClientId(Id), CancellationToken.None);
        logger.Lines.Count.ShouldEqual(2);
        var output = string.Join('\n', logger.Lines);
        output.ShouldContain("client.example");
        output.ShouldContain("None");
        output.ShouldContain("cache hit True");
        output.ShouldNotContain("untrusted-path");
        output.ShouldNotContain("synthetic-untrusted-name");
    }

    sealed class RecordingLogger : ILogger<ClientIdMetadataDocuments>
    {
        public List<string> Lines { get; } = [];
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Lines.Add(formatter(state, exception));
    }
}
