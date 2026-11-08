// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Erasing;

namespace Cratis.Identity.Accounts.for_ErasureConsumerReports;

public class when_no_consumer_is_listed : given.an_erasure_with_listed_consumers
{
    ErasureConsumerStatus _status;
    IReadOnlyList<ErasureConsumer> _defaultListing;

    async Task Because()
    {
        _defaultListing = new UnlistedErasureConsumers().Listed;
        _status = await new ErasureConsumerReports(new UnlistedErasureConsumers(), _acknowledgements).Report(_erasure);
    }

    [Fact] void should_list_nothing_by_default() => _defaultListing.ShouldBeEmpty();
    [Fact] void should_have_nothing_pending() => _status.Pending.ShouldBeEmpty();
}
