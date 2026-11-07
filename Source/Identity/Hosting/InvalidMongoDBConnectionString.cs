// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

/// <summary>
/// The exception that is thrown when the configured MongoDB connection string cannot be parsed. The driver's own
/// message can quote the whole string, credentials included, so this exception names the setting and nothing else.
/// </summary>
public class InvalidMongoDBConnectionString()
    : Exception("Identity:MongoDB:ConnectionString is not a valid MongoDB connection string. The value is not shown because it can contain credentials.");
