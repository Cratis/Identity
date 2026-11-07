// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Device;

namespace Cratis.Identity.for_DeviceGrantUserCode;

public class when_normalizing_human_input : Specification
{
    string _canonical;
    string _presented;
    string?[] _invalid;

    void Because()
    {
        _canonical = DeviceGrantUserCode.Normalize("BCDFGHJK");
        _presented = DeviceGrantUserCode.Normalize("bcdf-ghjk");
        _invalid = new[] { "BCDF0HJK", "BCDF1HJK", "BCDFOHJK", "BCDFIHJK", "BCDFGHJ", "BCDFGHJKL", "BCDF/GHJK", "BCDF\nGHJK", "ＢＣＤＦＧＨＪＫ", new string('B', 100) }.Select(_ => DeviceGrantUserCode.Normalize(_)).ToArray();
    }

    [Fact] void should_accept_the_generated_alphabet() => _canonical.ShouldEqual("BCDFGHJK");
    [Fact] void should_ignore_case_and_presentation_dashes() => _presented.ShouldEqual(_canonical);
    [Fact] void should_reject_ambiguous_non_ascii_and_malformed_input() => _invalid.All(_ => _ is null).ShouldBeTrue();
    [Fact] void should_preserve_more_than_32_bits_of_entropy() => (8 * Math.Log2(DeviceGrantUserCode.Alphabet.Distinct().Count()) > 32).ShouldBeTrue();
    [Fact] void should_have_no_repeated_alphabet_symbols() => DeviceGrantUserCode.Alphabet.Distinct().Count().ShouldEqual(DeviceGrantUserCode.Alphabet.Length);
}
