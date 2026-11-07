// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol.Stores;

public class when_attempting_to_write_configuration_stores : given.a_configuration_catalog
{
    readonly List<Exception> _errors = [];

    async Task Because()
    {
        var application = await Applications.FindByClientIdAsync("spec-api", CancellationToken.None);
        var scope = await Scopes.FindByNameAsync("spec:read", CancellationToken.None);
        _errors.Add(await Catch.Exception(async () => await Applications.CreateAsync(application, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Applications.UpdateAsync(application, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Applications.DeleteAsync(application, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Scopes.CreateAsync(scope, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Scopes.UpdateAsync(scope, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Scopes.DeleteAsync(scope, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Applications.SetClientSecretAsync(application, Secret, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Scopes.SetNameAsync(scope, Secret, CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Applications.InstantiateAsync(CancellationToken.None)));
        _errors.Add(await Catch.Exception(async () => await Scopes.InstantiateAsync(CancellationToken.None)));
    }

    [Fact] void should_reject_every_write() => _errors.TrueForAll(_ => _ is NotSupportedException).ShouldBeTrue();
    [Fact] void should_not_echo_the_secret_in_errors() => _errors.Exists(_ => _.ToString().Contains(Secret, StringComparison.Ordinal)).ShouldBeFalse();
}
