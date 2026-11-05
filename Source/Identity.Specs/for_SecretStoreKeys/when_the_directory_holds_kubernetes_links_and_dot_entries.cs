// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Keys;

namespace Cratis.Identity.for_SecretStoreKeys;

public class when_the_directory_holds_kubernetes_links_and_dot_entries : given.a_secret_store_directory
{
    IReadOnlyDictionary<string, string> _read;

    void Establish()
    {
        // Kubernetes mounts a timestamped directory, points ..data at it and links every key at the top level through ..data.
        var data = Path.Combine(_directory, "..2030_06_01");
        Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data, "signing.z.crt"), "linked");
        File.CreateSymbolicLink(Path.Combine(_directory, "signing.z.crt"), Path.Combine(data, "signing.z.crt"));
        File.WriteAllText(Path.Combine(_directory, ".hidden"), "ignored");
        Directory.CreateDirectory(Path.Combine(_directory, "subdirectory"));
        File.WriteAllText(Path.Combine(_directory, "subdirectory", "signing.y.crt"), "ignored");
    }

    void Because() => _read = SecretStoreDirectory.Read(_directory);

    [Fact] void should_follow_links_to_files() => _read["signing.z.crt"].ShouldEqual("linked");
    [Fact] void should_ignore_dot_entries() => _read.Keys.Any(_ => _.StartsWith('.')).ShouldBeFalse();
    [Fact] void should_ignore_sub_directories() => _read.ContainsKey("signing.y.crt").ShouldBeFalse();
}
