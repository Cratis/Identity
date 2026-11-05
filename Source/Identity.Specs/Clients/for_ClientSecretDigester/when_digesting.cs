// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.for_ClientSecretDigester;

public class when_digesting : Specification
{
    const string Secret = "hostile-ClientSecret-🦊";
    readonly ClientId _id = ClientId.Create("introspection");
    readonly ClientSecretDigester _digester = new(given.client_test_keys.Create(3));
    string _digest;

    void Because() => _digest = _digester.Digest(_id, Secret).Value;

    [Fact] void should_be_deterministic() => _digester.Digest(_id, Secret).Value.ShouldEqual(_digest);
    [Fact] void should_change_with_the_key() => new ClientSecretDigester(given.client_test_keys.Create(3, 43)).Digest(_id, Secret).Value.ShouldNotEqual(_digest);
    [Fact] void should_change_with_the_client_id() => _digester.Digest(ClientId.Create("another"), Secret).Value.ShouldNotEqual(_digest);
    [Fact] void should_have_the_key_revision() => _digest.StartsWith("v3.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_contain_the_secret() => _digest.Contains(Secret, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_contain_the_encoded_secret() => _digest.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes(Secret)), StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_be_unkeyed_hex() => _digest.ShouldNotEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Secret))));
    [Fact] void should_not_be_unkeyed_base64() => _digest.ShouldNotEqual(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(Secret))));
    [Fact] void should_separate_the_purpose() => _digest.ShouldNotEqual(OtherPurpose());

    string OtherPurpose()
    {
        using var framed = new MemoryStream();
        foreach (var part in new[] { "cratis.identity.upstream-key.v1", _id.Value, Secret })
        {
            var bytes = Encoding.UTF8.GetBytes(part);
            var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            framed.Write(length);
            framed.Write(bytes);
        }

        return "v3." + Convert.ToBase64String(given.client_test_keys.Create(3).Current.Hash(framed.ToArray())).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
