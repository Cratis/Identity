// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Keys;

namespace Cratis.Identity.for_KeyEligibility.given;

public class keys_at_a_moment : Specification
{
    protected FixedTimeProvider _time;
    private protected KeyEligibility _eligibility;
    protected Exception _exception;
    private protected RealmKey _current;

    void Establish()
    {
        _time = new FixedTimeProvider(KeyMaterial.Now);
        _eligibility = new KeyEligibility(_time);
    }

    protected static DateTimeOffset Ago(TimeSpan span) => KeyMaterial.Now - span;

    private protected void Select(params RealmKey[] keys) => _exception = Catch.Exception(() => _current = _eligibility.Current(KeyPurpose.Signing, keys));
}
