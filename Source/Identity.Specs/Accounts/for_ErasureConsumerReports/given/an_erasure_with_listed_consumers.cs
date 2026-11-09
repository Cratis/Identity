// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.for_ErasureConsumerReports.given;

public class an_erasure_with_listed_consumers : Specification
{
    private protected ErasureId _erasure;
    private protected FakeConsumers _consumers;
    private protected FakeAcknowledgements _acknowledgements;

    private protected ErasureConsumerReports Reports => new(_consumers, _acknowledgements);

    void Establish()
    {
        _erasure = ErasureId.New();
        _consumers = new([
            new("studio", ErasureConsumerDuty.DeleteAndAcknowledge),
            new("direct", ErasureConsumerDuty.DeleteAndAcknowledge),
            new("ante", ErasureConsumerDuty.RetentionOnly)]);
        _acknowledgements = new();
    }

    private protected class FakeConsumers(IReadOnlyList<ErasureConsumer> listed) : IErasureConsumers
    {
        public IReadOnlyList<ErasureConsumer> Listed => listed;
    }

    private protected class FakeAcknowledgements : IErasureAcknowledgements
    {
        readonly List<(ErasureId Erasure, ErasureConsumerId Consumer)> _recorded = [];

        public Task Record(ErasureId erasure, ErasureConsumerId consumer, DateTimeOffset at)
        {
            _recorded.Add((erasure, consumer));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ErasureConsumerId>> Acknowledged(ErasureId erasure) =>
            Task.FromResult<IReadOnlyCollection<ErasureConsumerId>>(_recorded.Where(entry => entry.Erasure == erasure).Select(entry => entry.Consumer).ToArray());
    }
}
