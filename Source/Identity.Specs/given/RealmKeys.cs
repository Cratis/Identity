// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.Keys;

namespace Cratis.Identity.given;

public static class RealmKeys
{
    public static RealmKey Signing(string kid, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var pair = KeyMaterial.Rsa(notBefore, notAfter);
        return new RealmKey(kid, kid, X509Certificate2.CreateFromPem(pair.Certificate), KeyPurpose.Signing, "RS256");
    }
}
