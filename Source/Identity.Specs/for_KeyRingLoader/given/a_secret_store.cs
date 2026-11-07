// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyRingLoader.given;

public class a_secret_store : Specification
{
    protected StoreFiles _files;
    private protected KeyRing _ring;
    protected Exception _exception;

    void Establish() => _files = new StoreFiles()
        .Pair("signing", "a", KeyMaterial.CurrentRsa())
        .Pair("encryption", "a", KeyMaterial.CurrentRsa())
        .DataProtection("a", KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddDays(90));

    protected void Load() => _exception = Catch.Exception(() => _ring = KeyRingLoader.Load(_files));

    private protected InvalidKeyMaterial Refusal => (InvalidKeyMaterial)_exception;
}
