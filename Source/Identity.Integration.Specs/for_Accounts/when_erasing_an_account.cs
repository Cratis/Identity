// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_Accounts;

[Collection(IdentityCollection.Name)]
public class when_erasing_an_account(IdentityHost host) : IAsyncLifetime
{
    const long GitHubId = 9100001;

    Account _original = default!;
    Account? _afterErasure;
    Account _signedInAgain = default!;
    long _personalDocuments;
    ErasedAccountDocument _marker = default!;
    long _markers;

    public async ValueTask InitializeAsync()
    {
        var accounts = new Accounts.Accounts(host.Database, TimeProvider.System);
        _original = await accounts.RecordGitHubSignIn(new GitHubUser(GitHubId, "erase-me", "Erase Me"));
        await accounts.Erase(_original.Id);
        await accounts.Erase(_original.Id);
        await accounts.Erase("never-existed-account");

        _afterErasure = await accounts.FindById(_original.Id);
        _personalDocuments = await host.Database.GetCollection<AccountDocument>(Accounts.Accounts.CollectionName).CountDocumentsAsync(_ => _.Id == _original.Id);
        var erased = host.Database.GetCollection<ErasedAccountDocument>(Accounts.Accounts.ErasedCollectionName);
        _markers = await erased.CountDocumentsAsync(_ => _.Id == _original.Id);
        _marker = await erased.Find(_ => _.Id == _original.Id).SingleAsync();
        _signedInAgain = await accounts.RecordGitHubSignIn(new GitHubUser(GitHubId, "erase-me", "Erase Me"));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_not_find_the_erased_account() => _afterErasure.ShouldBeNull();
    [Fact] void should_remove_the_personal_document() => _personalDocuments.ShouldEqual(0);
    [Fact] void should_keep_exactly_one_marker_however_often_it_is_erased() => _markers.ShouldEqual(1);
    [Fact] void should_record_when_it_was_erased() => (_marker.ErasedAt > DateTimeOffset.MinValue).ShouldBeTrue();
    [Fact] void should_create_a_new_account_when_the_github_user_signs_in_again() => _signedInAgain.Id.ShouldNotEqual(_original.Id);
}
