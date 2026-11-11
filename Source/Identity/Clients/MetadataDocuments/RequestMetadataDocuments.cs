// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Protocol.Stores;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Keeps a detached, validated snapshot for one request, including non-cacheable metadata.</summary>
/// <param name="documents">The issuer's bounded cross-request resolver.</param>
internal sealed class RequestMetadataDocuments(ClientIdMetadataDocuments documents) : IClientIdMetadataDocuments
{
    readonly Dictionary<string, Task<MetadataDocumentResult>> _snapshots = new(StringComparer.Ordinal);
    readonly Dictionary<string, string> _clientIds = new(StringComparer.Ordinal);
    readonly object _gate = new();

    /// <inheritdoc/>
    public async ValueTask<MetadataDocumentResult> Resolve(ClientId clientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task<MetadataDocumentResult> pending;
        lock (_gate)
        {
            if (!_snapshots.TryGetValue(clientId.Value, out pending))
            {
                pending = documents.Resolve(clientId, CancellationToken.None).AsTask();
                _snapshots.Add(clientId.Value, pending);
            }
        }

        var result = await pending.WaitAsync(cancellationToken);
        lock (_gate)
        {
            if (result.Descriptor is not null) _clientIds[new ClientApplication(result.Descriptor).Id] = clientId.Value;
            else if (_snapshots.TryGetValue(clientId.Value, out var current) && ReferenceEquals(current, pending)) _snapshots.Remove(clientId.Value);
        }

        return Copy(result);
    }

    /// <inheritdoc/>
    public string? ClientIdFor(string id)
    {
        lock (_gate)
        {
            return _clientIds.GetValueOrDefault(id) ?? documents.ClientIdFor(id);
        }
    }

    static MetadataDocumentResult Copy(MetadataDocumentResult result) => result.Descriptor is null ? result : result with { Descriptor = MetadataDescriptor.Copy(result.Descriptor) };
}
