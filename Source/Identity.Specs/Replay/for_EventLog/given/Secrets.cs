// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Replay.for_EventLog.given;

internal static class Secrets
{
    /// <summary>Synthetic, never valid authentication material.</summary>
    internal const string Access = "fake-upstream-access-material-for-scan";
    internal const string Refresh = "fake-upstream-refresh-material-for-scan";
    internal const string Code = "fake-authorization-code-for-scan";
    internal const string Client = "fake-client-secret-for-scan";
    internal const string Signing = "fake-signing-material-for-scan";
    internal const string Jwt = "eyJhbGciOiJub25lIn0.ZmFrZS1wYXlsb2Fk.signature";
    internal const string Pat = "id_pat_v1_abcdefghijklmnop_1234abcd";
    internal const string Opaque = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMN";
    internal static readonly string[] All = [Access, Refresh, Code, Client, Signing, Jwt, Pat, Opaque];
}
