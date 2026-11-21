// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.for_LinkProposal;

public class when_requiring_matching_proof : Specification
{
    LinkProposal _proposal;
    ValidationResult? _matching;
    ValidationResult? _mismatching;
    ValidationResult? _other_connection;
    ValidationResult? _other_issuer;

    void Establish() => _proposal = new(Guid.NewGuid(), new ConnectionId(Guid.NewGuid()), "https://github.com", "42", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, "pending_proof");

    void Because()
    {
        _matching = LinkProposal.RequireMatchingProof(_proposal, new(_proposal.Connection, _proposal.Issuer, _proposal.Subject, DateTimeOffset.UnixEpoch, FactId.New()));
        _mismatching = LinkProposal.RequireMatchingProof(_proposal, new(_proposal.Connection, _proposal.Issuer, "43", DateTimeOffset.UnixEpoch, FactId.New()));
        _other_connection = LinkProposal.RequireMatchingProof(_proposal, new(new ConnectionId(Guid.NewGuid()), _proposal.Issuer, _proposal.Subject, DateTimeOffset.UnixEpoch, FactId.New()));
        _other_issuer = LinkProposal.RequireMatchingProof(_proposal, new(_proposal.Connection, "https://other.example", _proposal.Subject, DateTimeOffset.UnixEpoch, FactId.New()));
    }

    [Fact] void should_accept_the_exact_upstream_tuple() => _matching.ShouldBeNull();
    [Fact] void should_refuse_a_different_subject() => _mismatching!.Message.ShouldEqual("upstream_proof_mismatch");
    [Fact] void should_refuse_a_different_connection() => _other_connection!.Message.ShouldEqual("upstream_proof_mismatch");
    [Fact] void should_refuse_a_different_issuer() => _other_issuer!.Message.ShouldEqual("upstream_proof_mismatch");
}
