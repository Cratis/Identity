// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Replay.for_EventLog.given;

internal static class EventRegistry
{
    internal static readonly Type[] Types = new[] { typeof(Account).Assembly, typeof(AccountSubject).Assembly }
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => Attribute.IsDefined(type, typeof(EventTypeAttribute))).ToArray();
}
