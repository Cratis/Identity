// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_UpstreamSubject;

public class when_validating_hostile_subjects
{
    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(13)]
    [InlineData(31)]
    [InlineData(127)]
    [InlineData(133)]
    void should_refuse_embedded_control_characters(int character) => Catch.Exception(() => _ = UpstreamSubject.Create($"immutable{(char)character}subject")).ShouldBeOfExactType<InvalidAccountValue>();

    [Theory]
    [InlineData('a', 257)]
    [InlineData('é', 129)]
    void should_refuse_subjects_over_the_utf8_byte_limit(char character, int count) => Catch.Exception(() => _ = UpstreamSubject.Create(new string(character, count))).ShouldBeOfExactType<InvalidAccountValue>();

    [Theory]
    [InlineData('a', 256)]
    [InlineData('é', 128)]
    void should_preserve_subjects_at_the_exact_utf8_byte_limit(char character, int count)
    {
        var subject = new string(character, count);
        UpstreamSubject.Create(subject).Value.ShouldEqual(subject);
    }
}
