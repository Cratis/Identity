// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb.Models;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.RevocationSpecs;

public class when_refreshing_with_an_inserted_successor : given.a_token_store
{
    readonly List<OpenIddictMongoDbToken> _entries = [];
    IOpenIddictTokenManager _tokens;
    IOpenIddictAuthorizationManager _authorizations;
    TimeProvider _time;
    ProcessSignInContext _first;
    ProcessSignInContext _second;
    OpenIddictMongoDbToken _successor;
    GrantState _after_first;
    GrantState _after_second;
    RefreshIssuance _second_issuance;

    void Establish()
    {
        // OpenIddict's CreateTokenEntry persists GenerateTokenContext.TokenType, not a token_type_hint.
        _entry.Type = TokenTypeIdentifiers.RefreshToken;
        _entry.Status = Statuses.Valid;
        _collection.InsertOneAsync(Arg.Any<OpenIddictMongoDbToken>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _entries.Add(call.Arg<OpenIddictMongoDbToken>());
                return Task.CompletedTask;
            });
        _tokens = Substitute.For<IOpenIddictTokenManager>();
        _tokens.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new ValueTask<object?>(_entries.SingleOrDefault(entry => entry.Id.ToString() == call.Arg<string>())));
        _tokens.GetExpirationDateAsync(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(call => new DateTimeOffset?(((OpenIddictMongoDbToken)call.Arg<object>()).ExpirationDate!.Value));
        _tokens.HasStatusAsync(Arg.Any<object>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => ((OpenIddictMongoDbToken)call.Arg<object>()).Status == call.Arg<string>());
        _tokens.TryRedeemAsync(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            ((OpenIddictMongoDbToken)call.Arg<object>()).Status = Statuses.Redeemed;
            return new ValueTask<bool>(true);
        });
        _tokens.FindByAuthorizationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Entries());
        _authorizations = Substitute.For<IOpenIddictAuthorizationManager>();
        _time = Substitute.For<TimeProvider>();
        _time.GetUtcNow().Returns(DateTimeOffset.UnixEpoch);
        _second_issuance = new();
    }

    async Task Because()
    {
        await _fences.RecordFamilyStart(_entry.AuthorizationId.ToString(), _entry.Id.ToString(), _entry.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30), CancellationToken.None);
        await _store.CreateAsync(_entry, CancellationToken.None);
        _first = await Refresh(_entry, _issuance);
        _successor = new OpenIddictMongoDbToken
        {
            Id = ObjectId.GenerateNewId(),
            AuthorizationId = _entry.AuthorizationId,
            Type = TokenTypeIdentifiers.RefreshToken,
            Status = Statuses.Valid,
            Subject = _entry.Subject,
            CreationDate = DateTime.UnixEpoch.AddDays(1),
            ExpirationDate = DateTime.UnixEpoch.AddDays(31)
        };
        await _store.CreateAsync(_successor, CancellationToken.None);
        _after_first = await _fences.Read(_entry.AuthorizationId.ToString(), CancellationToken.None);
        _second = await Refresh(_successor, _second_issuance);
        _after_second = await _fences.Read(_entry.AuthorizationId.ToString(), CancellationToken.None);
    }

    async Task<ProcessSignInContext> Refresh(OpenIddictMongoDbToken token, RefreshIssuance issuance)
    {
        var transaction = new OpenIddictServerTransaction
        {
            EndpointType = OpenIddictServerEndpointType.Token,
            Request = new OpenIddictRequest { GrantType = GrantTypes.RefreshToken }
        };
        transaction.SetProperty(typeof(ProcessAuthenticationContext).FullName, new ProcessAuthenticationContext(transaction)
        {
            RefreshTokenPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"))
                .SetAuthorizationId(token.AuthorizationId.ToString()).SetTokenId(token.Id.ToString())
        });
        var context = new ProcessSignInContext(transaction);
        var grants = new GrantRevocation(_tokens, _authorizations, _fences, FenceDoubles.Confirmation(_fences));
        var read = new ReadRefreshGrant(_fences, _tokens, _authorizations, _time);
        await new RedeemRefreshTokenOnce(_tokens, grants, _time, _fences, read, issuance).HandleAsync(context);
        return context;
    }

    async IAsyncEnumerable<object> Entries()
    {
        foreach (var entry in _entries)
        {
            yield return entry;
        }

        await Task.CompletedTask;
    }

    [Fact] void should_accept_the_first_refresh() => _first.IsRejected.ShouldBeFalse();
    [Fact] void should_bind_admission_to_the_inserted_successor() => _after_first.CurrentRefreshTokenId.ShouldEqual(_successor.Id.ToString());
    [Fact] void should_accept_the_successors_first_use() => _second.IsRejected.ShouldBeFalse();
    [Fact] void should_admit_the_second_refresh() => _second_issuance.Admission.ShouldNotBeNull();
    [Fact] void should_install_the_next_successor() => _after_second.CurrentRefreshTokenId.ShouldEqual(_second_issuance.SuccessorId);
    [Fact] void should_not_revoke_the_family() => _after_second.Fence.ShouldBeNull();
    [Fact] void should_record_only_one_family_start() => _after_second.Outbox.Count(entry => entry.Kind == FactKind.FamilyStarted).ShouldEqual(1);
    [Fact] void should_preserve_the_original_absolute_expiry() => _successor.ExpirationDate.ShouldEqual(_entry.ExpirationDate);
}
