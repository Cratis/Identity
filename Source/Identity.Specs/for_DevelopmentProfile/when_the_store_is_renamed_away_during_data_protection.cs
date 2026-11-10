// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Identity.Development;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_store_is_renamed_away_during_data_protection : given.a_renamed_store
{
    string[] _elements;

    void Because() => WithRenamedStore(() =>
    {
        var repository = new DevelopmentXmlRepository(_custody);
        repository.StoreElement(new XElement("key", new XAttribute("id", "custody-spec")), "key-custody-spec");
        _elements = [.. repository.GetAllElements().Select(_ => _.ToString(SaveOptions.DisableFormatting))];
    });

    [Fact(SkipUnless = nameof(HasHandleRelativeCustody), SkipType = typeof(given.a_renamed_store), Skip = "Requires Windows, macOS or Linux directory handles.")]
    void should_read_and_publish_xml_only_in_the_pinned_child_directory()
    {
        _exception.ShouldBeNull();
        _elements.ShouldContainOnly("<key id=\"custody-spec\" />");
        _decoyXml.ShouldEqual(DecoyXml);
        new DevelopmentXmlRepository(_custody).GetAllElements().Count.ShouldEqual(1);
    }
}
