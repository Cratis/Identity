// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>Domain-separated, length-framed HMAC over exact immutable upstream values.</summary>
/// <param name="keys">The lookup key source.</param>
public class UpstreamKeyDigester(IUpstreamLookupKeys keys)
{
    /// <summary>Computes a revision-prefixed keyed pseudonym.</summary>
    /// <param name="issuer">The exact authority.</param>
    /// <param name="subject">The validated subject.</param>
    /// <returns>The lookup pseudonym.</returns>
    public UpstreamLookupDigest Digest(UpstreamIssuer issuer, UpstreamSubject subject)
    {
        var authority = Encoding.UTF8.GetBytes(issuer.Value);
        var upstream = Encoding.UTF8.GetBytes(subject.Value);
        using var input = new MemoryStream();
        input.Write(Encoding.UTF8.GetBytes("cratis.identity.upstream-key-lookup.v1"));
        Frame(input, authority);
        Frame(input, upstream);
        var hash = Convert.ToBase64String(keys.Current.Hash(input.ToArray())).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new($"v{keys.Current.Revision.ToString(CultureInfo.InvariantCulture)}.{hash}");
    }

    static void Frame(Stream stream, byte[] bytes)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        stream.Write(length);
        stream.Write(bytes);
    }
}
