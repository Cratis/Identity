// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cratis.Identity.Authorization.RateLimiting;

/// <summary>Extracts bounded, untrusted request partition hints without authenticating them.</summary>
internal static class TokenRequestIdentification
{
    /// <summary>Reads cached form data and the connection's normalized source address.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The partition hints.</returns>
    internal static async Task<Identification> Read(HttpContext context)
    {
        var source = Source(context.Connection.RemoteIpAddress);
        if (!HttpMethods.IsPost(context.Request.Method)) return new(null, null, source);

        var basicClient = BasicClient(context.Request.Headers.Authorization.ToString());
        try
        {
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync(context.RequestAborted);
                var client = basicClient ?? SafeValue(form["client_id"].Count == 1 ? form["client_id"].ToString() : null);
                var grant = SafeValue(form["grant_type"].Count == 1 ? form["grant_type"].ToString() : null);
                return new(client, grant, source);
            }
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or BadHttpRequestException or InvalidOperationException)
        {
            // Leave protocol errors to OpenIddict; the source budget still applies.
        }

        return new(basicClient, null, source);
    }

    static string? BasicClient(string authorization)
    {
        // Bound transient decoding as well as retained keys. Long credentials remain source-limited.
        if (authorization.Length > 4096 || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return null;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..]));
            var separator = decoded.IndexOf(':');
            return separator < 0 ? null : SafeValue(Uri.UnescapeDataString(decoded[..separator].Replace('+', ' ')));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    static string? SafeValue(string? value) => string.IsNullOrEmpty(value) || value.Length > 256 || value.Any(char.IsControl) ? null : value;

    static string Source(IPAddress? address)
    {
        if (address is null) return "unknown";
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return address.ToString();
        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new IPAddress(bytes)}/64";
    }

    /// <summary>Untrusted, bounded admission hints.</summary>
    /// <param name="ClientId">The claimed client, or null.</param>
    /// <param name="GrantType">The requested grant, or null.</param>
    /// <param name="Source">The normalized connection source.</param>
    internal readonly record struct Identification(string? ClientId, string? GrantType, string Source);
}
