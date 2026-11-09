// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>A consumer listed for erasure follow-up.</summary>
/// <param name="Id">The consumer.</param>
/// <param name="Duty">What is expected of it.</param>
internal record ErasureConsumer(ErasureConsumerId Id, ErasureConsumerDuty Duty);
