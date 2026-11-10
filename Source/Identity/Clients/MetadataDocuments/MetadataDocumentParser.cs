// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Validates the public code/S256-only subset of CIMD draft -03.</summary>
/// <param name="options">Issuer-owned limits.</param>
internal sealed class MetadataDocumentParser(ClientIdMetadataOptions options)
{
    static readonly UTF8Encoding _utf8 = new(false, true);

    /// <summary>Parses a complete bounded UTF-8 document without optional URL fetching.</summary>
    /// <param name="clientId">The exact fetch identity.</param>
    /// <param name="bytes">The response bytes.</param>
    /// <returns>A validated descriptor or a reason-only refusal.</returns>
    public MetadataDocumentResult Parse(string clientId, ReadOnlyMemory<byte> bytes)
    {
        if (!ClientIdUrlPolicy.IsValid(clientId)) return MetadataDocumentResult.Refused(MetadataRefusal.InvalidUrl);
        if (bytes.Length > options.MaxDocumentBytes) return MetadataDocumentResult.Refused(MetadataRefusal.Size);
        try
        {
            _utf8.GetCharCount(bytes.Span);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || HasDuplicateKeys(root)) return MetadataDocumentResult.Refused(MetadataRefusal.Json);
            var refusal = MetadataFields.Validate(root, clientId);
            return refusal == MetadataRefusal.None ? Build(root, clientId) : MetadataDocumentResult.Refused(refusal);
        }
        catch (JsonException)
        {
            return MetadataDocumentResult.Refused(MetadataRefusal.Json);
        }
        catch (DecoderFallbackException)
        {
            return MetadataDocumentResult.Refused(MetadataRefusal.Json);
        }
    }

    static bool HasDuplicateKeys(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().Select(_ => _.Name).Distinct(StringComparer.Ordinal).Count() != element.GetPropertyCount() ||
            element.EnumerateObject().Any(_ => HasDuplicateKeys(_.Value)),
        JsonValueKind.Array => element.EnumerateArray().Any(HasDuplicateKeys),
        _ => false
    };

    MetadataDocumentResult Build(JsonElement root, string clientId)
    {
        if (!MetadataRedirects.TryParse(root, options, out var redirects, out var native)) return MetadataDocumentResult.Refused(MetadataRefusal.Metadata);
        var metadata = new ClientIdMetadataDocument(new Uri(clientId), MetadataClientName.Sanitize(MetadataFields.String(root, "client_name"), new Uri(clientId).IdnHost), redirects)
        {
            Native = native,
            AllowsRefreshToken = MetadataFields.AllowsRefresh(root)
        };
        var descriptor = MetadataDescriptor.Create(metadata);
        var validation = ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256], options.AllowLocalhostLoopback);
        return validation.IsValid ? new(descriptor, MetadataRefusal.None) : MetadataDocumentResult.Refused(MetadataRefusal.Metadata);
    }
}
