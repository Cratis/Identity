// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>One bounded positive or in-flight entry, including its reverse-ID mapping.</summary>
/// <param name="key">The exact validated identity.</param>
/// <param name="access">The access order.</param>
internal sealed class MetadataDocumentCacheEntry(string key, long access)
{
    /// <summary>Gets the exact cache key.</summary>
    public string Key { get; } = key;

    /// <summary>Gets the shared completion, with asynchronous continuations.</summary>
    public TaskCompletionSource<MetadataDocumentResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets or sets the last access order.</summary>
    public long Access { get; set; } = access;

    /// <summary>Gets or sets the validated revision's expiry.</summary>
    public DateTimeOffset Expires { get; set; }

    /// <summary>Gets or sets the reverse ID only after validation succeeds.</summary>
    public string? Id { get; set; }
}
