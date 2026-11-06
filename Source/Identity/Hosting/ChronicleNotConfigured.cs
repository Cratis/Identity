// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>Identity account admission requires a configured native Chronicle client.</summary>
public sealed class ChronicleNotConfigured() : Exception("Cratis Identity requires a configured Chronicle client.");
