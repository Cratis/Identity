// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding;

public class when_substituting_another_authorization_request : given.an_activation
{
    ConsentRequest? _substitutedApproval;
    ConsentRequest? _reverseSubstitution;
    string? _substitutedCode;
    string? _originalCode;

    async Task Because()
    {
        var otherBinding = binding with { AuthorizationRequest = AuthorizationRequestId.New() };
        var other = await activation.Create(otherBinding, CancellationToken.None);
        _substitutedApproval = await activation.Decide(request.Id, otherBinding, true, CancellationToken.None);
        _reverseSubstitution = await activation.Decide(other.Id, binding, true, CancellationToken.None);
        await Approve();
        await Resume();
        _substitutedCode = await activation.ClaimCode(request.Id, otherBinding, CancellationToken.None);
        _originalCode = await Claim();
    }

    [Fact] void should_reject_substituting_the_other_protected_request() => _substitutedApproval.ShouldBeNull();
    [Fact] void should_reject_the_reverse_substitution() => _reverseSubstitution.ShouldBeNull();
    [Fact] void should_reject_a_cross_request_code_claim() => _substitutedCode.ShouldBeNull();
    [Fact] void should_not_consume_the_original_request_code_permission() => _originalCode.ShouldNotBeNull();
}
