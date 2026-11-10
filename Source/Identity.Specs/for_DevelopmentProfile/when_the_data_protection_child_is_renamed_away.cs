// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Identity.Development;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_data_protection_child_is_renamed_away : given.a_renamed_store
{
    string[] _elements;
    string _decoy;

    void Because()
    {
        if (!HasHandleRelativeCustody)
        {
            return;
        }

        Apply();
        _exception.ShouldBeNull();
        var repository = new DevelopmentXmlRepository(_custody);
        var child = Path.Combine(_storeDirectory, DevelopmentKeyStore.DataProtectionDirectory);
        var moved = child + "-validated";
        Directory.Move(child, moved);
        try
        {
            Directory.CreateDirectory(child);
            File.WriteAllText(Path.Combine(child, "key-decoy.xml"), DecoyXml);
            repository.StoreElement(new XElement("key", new XAttribute("id", "child-custody-spec")), "key-child-custody-spec");
            _elements = [.. repository.GetAllElements().Select(_ => _.ToString(SaveOptions.DisableFormatting))];
            _decoy = File.ReadAllText(Path.Combine(child, "key-decoy.xml"));
        }
        finally
        {
            Directory.Delete(child, recursive: true);
            Directory.Move(moved, child);
        }
    }

    [Fact(SkipUnless = nameof(HasHandleRelativeCustody), SkipType = typeof(given.a_renamed_store), Skip = "Requires Windows, macOS or Linux directory handles.")]
    void should_keep_using_the_child_pinned_during_provisioning()
    {
        _elements.ShouldContainOnly("<key id=\"child-custody-spec\" />");
        _decoy.ShouldEqual(DecoyXml);
    }
}
