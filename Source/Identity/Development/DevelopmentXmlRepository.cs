// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Cratis.Identity.Development;

/// <summary>
/// Persists the local Data Protection ring through the same process-lifetime directory custody as the signing keys.
/// The child directory is pinned during provisioning; the file-system repository's pathname-based opens are never used.
/// </summary>
internal sealed class DevelopmentXmlRepository(DevelopmentKeyStoreRoot root) : IXmlRepository
{
    readonly DevelopmentKeyStoreRoot _directory = root.GetDirectory(DevelopmentKeyStore.DataProtectionDirectory);

    /// <inheritdoc/>
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        root.RequireMarker();
        return _directory.Entries().Select(ReadElement).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public void StoreElement(XElement element, string friendlyName)
    {
        root.RequireMarker();
        var name = string.IsNullOrWhiteSpace(friendlyName) ? Guid.NewGuid().ToString("N") : friendlyName;
        _directory.WriteAtomically($"{name}.xml", element.ToString(SaveOptions.DisableFormatting));
    }

    XElement ReadElement(string name)
    {
        if (!name.EndsWith(".xml", StringComparison.Ordinal))
        {
            throw new DevelopmentProfileRefused($"the Data Protection directory holds an unexpected entry ({name}); remove the key directory so it can be recreated.");
        }

        using var stream = _directory.OpenRead(name);
        return XElement.Load(stream);
    }
}
