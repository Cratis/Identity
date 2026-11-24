// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Identity.Clients.Serving.for_RegisteredClientSecrets;

public class when_validating_a_presented_secret : given.client_commands
{
    const string Previous = "synthetic-previous-secret";
    const string Rotated = "synthetic-rotated-secret-for-specs";

    ClientRegistration _rotated;
    ClientRegistration _retired;

    void Establish()
    {
        _rotated = _active with
        {
            SecretHash = _digester.Digest(Rotated),
            ActivatedAt = Now,
            PreviousSecretRevisions = [new(_digester.Digest(Previous), Now.AddDays(-1), Now.AddHours(1))]
        };
        _retired = _rotated with { Retired = true };
    }

    [Fact] void should_accept_the_current_secret() => RegisteredClientSecrets.IsValid(_rotated, Rotated, _digester, Now).ShouldBeTrue();
    [Fact] void should_accept_the_previous_secret_while_the_overlap_lasts() => RegisteredClientSecrets.IsValid(_rotated, Previous, _digester, Now.AddMinutes(30)).ShouldBeTrue();
    [Fact] void should_refuse_the_previous_secret_after_the_overlap() => RegisteredClientSecrets.IsValid(_rotated, Previous, _digester, Now.AddHours(2)).ShouldBeFalse();
    [Fact] void should_refuse_a_secret_that_was_never_issued() => RegisteredClientSecrets.IsValid(_rotated, "something-else-entirely", _digester, Now).ShouldBeFalse();
    [Fact] void should_refuse_an_empty_secret() => RegisteredClientSecrets.IsValid(_rotated, string.Empty, _digester, Now).ShouldBeFalse();
    [Fact] void should_refuse_the_new_secret_before_it_is_active() => RegisteredClientSecrets.IsValid(_rotated, Rotated, _digester, Now.AddMinutes(-1)).ShouldBeFalse();
    [Fact] void should_refuse_every_secret_of_a_retired_client() => RegisteredClientSecrets.IsValid(_retired, Rotated, _digester, Now).ShouldBeFalse();
}
