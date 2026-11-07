// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Identity.for_SecretStoreXmlRepository;

public class when_the_secret_store_is_reloaded : given.a_data_protection_ring
{
    string _original;
    string _added;
    string _afterAdding;
    string _afterRemoving;
    IDataProtector _protector;

    void Establish()
    {
        // Data Protection reads the real clock, so this spec lives in real time.
        var now = DateTimeOffset.UtcNow;
        _time.Now = now;
        _files = new StoreFiles()
            .Pair("signing", "a", KeyMaterial.Rsa(now.AddDays(-1), now.AddDays(30)))
            .Pair("encryption", "a", KeyMaterial.Rsa(now.AddDays(-1), now.AddDays(30)))
            .DataProtection("a", now.AddDays(-1), now.AddDays(90));
        Open();
        _protector = Provider().CreateProtector("spec");
        _original = KeyIdOf(_protector.Protect([1, 2, 3]));
    }

    void Because()
    {
        // A newer key is provisioned; ASP.NET Core would otherwise keep the cached ring for a day.
        _files.DataProtection("b", _time.Now.AddHours(-1), _time.Now.AddDays(90));
        _files.WriteTo(_directory);
        _keys.Reload();
        var newKey = _keys.Current.Ring.DataProtection.Single(_ => _.Name == "b").Element.Attribute("id").Value;
        _afterAdding = KeyIdOf(_protector.Protect([1, 2, 3]));
        _added = newKey;

        // It is then retired: removed from the directory.
        File.Delete(Path.Combine(_directory, "data-protection.b.xml"));
        _keys.Reload();
        _afterRemoving = KeyIdOf(_protector.Protect([1, 2, 3]));
    }

    static string KeyIdOf(byte[] payload) => new Guid(payload.AsSpan(4, 16)).ToString();

    [Fact] void should_start_with_the_first_key() => _original.ShouldNotEqual(_added);
    [Fact] void should_protect_with_a_newly_added_key_straight_after_the_reload() => _afterAdding.ShouldEqual(_added);
    [Fact] void should_stop_using_a_removed_key_straight_after_the_reload() => _afterRemoving.ShouldEqual(_original);
}
