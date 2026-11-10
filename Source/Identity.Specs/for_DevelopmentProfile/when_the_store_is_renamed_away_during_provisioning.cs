// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_store_is_renamed_away_during_provisioning : given.a_renamed_store
{
    string _validatedMarker;
    int _validatedKeys;
    string _replacementMarker;

    void Because() => WithRenamedStore(() =>
    {
        _validatedMarker = File.ReadAllText(Path.Combine(_movedDirectory, DevelopmentKeyStore.MarkerFile));
        _validatedKeys = _custody.ReadKeyFiles().Count;
        _replacementMarker = File.ReadAllText(Path.Combine(_storeDirectory, DevelopmentKeyStore.MarkerFile));
    });

    [Fact(SkipUnless = nameof(HasHandleRelativeCustody), SkipType = typeof(given.a_renamed_store), Skip = "Requires Windows, macOS or Linux directory handles.")] void should_provision_only_the_validated_directory_and_leave_the_substitute_untouched_after_restore()
    {
        _exception.ShouldBeNull();
        _validatedMarker.ShouldEqual(DevelopmentKeyStore.Marker);
        _validatedKeys.ShouldEqual(4);
        _replacementMarker.ShouldEqual(DecoyMarker);
        _decoyEntries.ShouldContainOnly(DevelopmentKeyStore.MarkerFile, DevelopmentKeyStore.DataProtectionDirectory);
        _custody.ReadKeyFiles().Count.ShouldEqual(4);
    }
}
