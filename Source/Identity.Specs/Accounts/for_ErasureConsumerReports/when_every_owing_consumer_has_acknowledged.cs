// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_ErasureConsumerReports;

public class when_every_owing_consumer_has_acknowledged : given.an_erasure_with_listed_consumers
{
    ErasureConsumerStatus _status;

    async Task Establish()
    {
        await _acknowledgements.Record(_erasure, "studio", DateTimeOffset.UnixEpoch);
        await _acknowledgements.Record(_erasure, "direct", DateTimeOffset.UnixEpoch);
        await _acknowledgements.Record(_erasure, "direct", DateTimeOffset.UnixEpoch);
        await _acknowledgements.Record(_erasure, "unlisted", DateTimeOffset.UnixEpoch);
        await _acknowledgements.Record(ErasureId.New(), "ante", DateTimeOffset.UnixEpoch);
    }

    async Task Because() => _status = await Reports.Report(_erasure);

    [Fact] void should_be_settled() => _status.Settled.ShouldBeTrue();
    [Fact] void should_report_each_owing_consumer_once() => _status.Acknowledged.ShouldContainOnly([new ErasureConsumerId("studio"), new ErasureConsumerId("direct")]);
    [Fact] void should_ignore_unlisted_consumers_and_other_erasures() => _status.Pending.ShouldBeEmpty();
}
