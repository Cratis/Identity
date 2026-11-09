// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Erasing;

namespace Cratis.Identity.Accounts.for_ErasureConsumerReports;

public class when_no_consumer_has_acknowledged : given.an_erasure_with_listed_consumers
{
    ErasureConsumerStatus _status;

    async Task Because() => _status = await Reports.Report(_erasure);

    [Fact] void should_not_be_settled() => _status.Settled.ShouldBeFalse();
    [Fact] void should_list_the_consumers_owing_an_acknowledgement() => _status.Pending.ShouldContainOnly([new ErasureConsumerId("studio"), new ErasureConsumerId("direct")]);
    [Fact] void should_list_the_retention_only_consumer_separately() => _status.RetentionOnly.ShouldContainOnly([new ErasureConsumerId("ante")]);
    [Fact] void should_have_no_acknowledged_consumer() => _status.Acknowledged.ShouldBeEmpty();
}
