// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>What Identity expects of a listed consumer once an account is erased.</summary>
internal enum ErasureConsumerDuty
{
    /// <summary>The consumer deletes its copies and acknowledges.</summary>
    DeleteAndAcknowledge = 0,

    /// <summary>The consumer is documented as retaining data; no acknowledgement is expected.</summary>
    RetentionOnly = 1
}
