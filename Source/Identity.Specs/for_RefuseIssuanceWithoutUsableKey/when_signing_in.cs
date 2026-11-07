// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RefuseIssuanceWithoutUsableKey;

public class when_signing_in : for_SecretStoreKeys.given.a_secret_store_directory
{
    ProcessSignInContext _usable;
    ProcessSignInContext _expired;

    void Establish()
    {
        new StoreFiles().Pair("signing", "a", KeyMaterial.Rsa(KeyMaterial.Now.AddDays(-1), KeyMaterial.Now.AddHours(1))).WriteTo(_directory);
        Open();
    }

    async Task Because()
    {
        var handler = new RefuseIssuanceWithoutUsableKey(_keys);
        _usable = Context();
        await handler.HandleAsync(_usable);
        _time.Now = KeyMaterial.Now.AddHours(2);
        _expired = Context();
        await handler.HandleAsync(_expired);
    }

    static ProcessSignInContext Context() => new(new OpenIddictServerTransaction
    {
        EndpointType = OpenIddictServerEndpointType.Token,
        Logger = NullLogger.Instance,
        Options = new OpenIddictServerOptions(),
        Request = new OpenIddictRequest()
    });

    [Fact] void should_allow_issuance_with_a_valid_key() => _usable.IsRejected.ShouldBeFalse();
    [Fact] void should_refuse_issuance_once_the_signing_key_expired() => _expired.IsRejected.ShouldBeTrue();
    [Fact] void should_report_a_server_error() => _expired.Error.ShouldEqual(Errors.ServerError);
    [Fact]
    void should_run_before_the_sign_in_demand_and_so_before_a_refresh_token_is_redeemed() =>
        RefuseIssuanceWithoutUsableKey.Descriptor.Order.ShouldBeLessThan(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order);
}
