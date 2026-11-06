// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol;

/// <summary>Marks a command that may only execute through the verified protocol boundary.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProtocolCommandAttribute : Attribute;
