// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_store_is_renamed_away_during_key_reload : given.a_renamed_store
{
    string _initialKid;
    string _reloadedKid;
    bool _canIssue;

    void Because() => WithRenamedStore(() =>
    {
        using var keys = new SecretStoreKeys(
            new SecretStoreOptions { Path = _storeDirectory },
            TimeProvider.System,
            NullLogger<SecretStoreKeys>.Instance,
            providesDataProtection: false,
            developmentRoot: _custody);
        _initialKid = keys.Current.Signer.Kid;
        keys.Reload();
        _reloadedKid = keys.Current.Signer.Kid;
        _canIssue = keys.CanIssue;
    });

    [Fact(SkipUnless = nameof(HasHandleRelativeCustody), SkipType = typeof(given.a_renamed_store), Skip = "Requires Windows, macOS or Linux directory handles.")] void should_load_and_reload_the_pinned_keys_while_the_original_name_points_at_an_invalid_store()
    {
        _exception.ShouldBeNull();
        _initialKid.ShouldNotBeNull();
        _reloadedKid.ShouldEqual(_initialKid);
        _canIssue.ShouldBeTrue();
    }
}
