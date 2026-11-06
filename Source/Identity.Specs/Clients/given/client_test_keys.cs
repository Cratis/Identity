// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Secrets;

namespace Cratis.Identity.Clients.given;

public static class client_test_keys
{
    public static IClientSecretDigestKeys Create(int revision = 1, byte fill = 42)
    {
        var keys = Substitute.For<IClientSecretDigestKeys>();
        keys.Current.Returns(new ClientSecretDigestKey(revision, Enumerable.Repeat(fill, 32).ToArray()));
        return keys;
    }
}
