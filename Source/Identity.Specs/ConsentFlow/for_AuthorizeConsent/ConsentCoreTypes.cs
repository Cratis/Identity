// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Identity.ConsentFlow.for_AuthorizeConsent;

static class ConsentCoreTypes
{
    internal static IEnumerable<string> In(object live) => live.GetType().GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .SelectMany(_ => _.GetParameters())
        .Select(_ => _.ParameterType)
        .Where(_ => _.Namespace == typeof(ConsentActivation).Namespace)
        .Select(_ => _.Name);
}
