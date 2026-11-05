// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_answers_for_another_subject : given.a_tenants_callback
{
    void Establish() => handler.Body = /*lang=json*/ """{"subject":"ACCOUNT-1","tenants":[]}""";
    Task Because() => Lookup();

    [Fact] void should_refuse_the_answer() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.WrongSubject));
}
