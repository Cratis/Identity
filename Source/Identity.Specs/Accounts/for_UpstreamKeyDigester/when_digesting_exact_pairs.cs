// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamKeyDigester;

public class when_digesting_exact_pairs : Specification
{
    UpstreamKeyDigester _digester;
    UpstreamLookupDigest _result;

    void Establish()
    {
        var keys = Substitute.For<IUpstreamLookupKeys>();
        keys.Current.Returns(new UpstreamLookupKey(1, Enumerable.Range(0, 32).Select(number => (byte)number).ToArray()));
        _digester = new(keys);
    }

    void Because() => _result = _digester.Digest("https://github.com", "12345");

    [Fact] void should_match_the_framed_hmac_vector() => _result.Value.ShouldEqual("v1.pnic9IIY-2XYkqbxvd1DL9vf2dCjWgZiqVvXqnyobBY");
    [Fact] void should_preserve_issuer_case() => _digester.Digest("https://GitHub.com", "12345").ShouldNotEqual(_result);
    [Fact] void should_preserve_issuer_slash() => _digester.Digest("https://github.com/", "12345").ShouldNotEqual(_result);
    [Fact] void should_preserve_subject_case() => _digester.Digest("https://github.com", "ABC").ShouldNotEqual(_digester.Digest("https://github.com", "abc"));
    [Fact] void should_frame_component_boundaries() => _digester.Digest("https://a.test/b", "c").ShouldNotEqual(_digester.Digest("https://a.test/", "bc"));
}
