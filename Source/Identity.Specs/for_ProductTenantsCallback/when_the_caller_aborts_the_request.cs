// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_caller_aborts_the_request : given.a_tenants_callback
{
    [Fact]
    async Task should_propagate_cancellation()
    {
        using var aborted = new CancellationTokenSource();
        handler.BeforeResponse = async token =>
        {
            await aborted.CancelAsync();
            token.ThrowIfCancellationRequested();
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Lookup(aborted.Token));
    }
}
