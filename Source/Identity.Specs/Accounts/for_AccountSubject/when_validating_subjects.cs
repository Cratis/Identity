// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_AccountSubject;

public class when_validating_subjects
{
    [Theory]
    [InlineData("8B167AD2-0F89-4C78-9D42-6D4580495D32")]
    [InlineData("8b167ad20f894c789d426d4580495d32")]
    [InlineData("8b167ad2-0f89-1c78-9d42-6d4580495d32")]
    [InlineData("")]
    [InlineData("octocat")]
    void should_refuse_noncanonical_subjects(string value) => AccountSubject.TryParse(value, out _).ShouldBeFalse();

    [Fact]
    void should_accept_canonical_subject() => AccountSubject.TryParse("8b167ad2-0f89-4c78-9d42-6d4580495d32", out _).ShouldBeTrue();
}
