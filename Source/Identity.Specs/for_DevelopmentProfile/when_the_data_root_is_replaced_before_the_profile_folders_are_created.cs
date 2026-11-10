// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_data_root_is_replaced_before_the_profile_folders_are_created : given.a_renamed_store
{
    string _movedRoot;
    string[] _substituteEntries;
    bool _moved;

    void Because()
    {
        if (!HasHandleRelativeCustody)
        {
            return;
        }

        _movedRoot = _root + "-pinned";
        var store = new DevelopmentKeyStore(
            _root,
            _contentRoot,
            TimeProvider.System,
            beforeDirectoryCreation: _ =>
            {
                if (_moved)
                {
                    return;
                }

                // Synchronous seam: the data root is pinned but nothing below it exists yet. Swap the pathname for a substitute.
                _moved = true;
                Directory.Move(_root, _movedRoot);
                Directory.CreateDirectory(_root);
            });
        _exception = Catch.Exception(() =>
        {
            store.Provision(StoreName);
            _custodies.Add(store.Custody);
        });
        _substituteEntries = [.. Directory.EnumerateFileSystemEntries(_root)];
    }

    [Fact(SkipUnless = nameof(HasHandleRelativeCustody), SkipType = typeof(given.a_renamed_store), Skip = "Requires Windows, macOS or Linux directory handles.")]
    void should_create_every_profile_folder_only_inside_the_pinned_root_and_leave_the_substitute_untouched()
    {
        _exception.ShouldBeNull();
        _substituteEntries.ShouldBeEmpty();
        Directory.Exists(Path.Combine(_movedRoot, "cratis", "identity-dev", StoreName)).ShouldBeTrue();
    }
}
