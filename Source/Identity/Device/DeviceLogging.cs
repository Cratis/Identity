// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Device;

/// <summary>Logs device flow outcomes without request values or credentials.</summary>
internal static partial class DeviceLogging
{
    [LoggerMessage(LogLevel.Warning, "Device authorization throttle refused a request")]
    internal static partial void ThrottleRefused(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Device authorization polling interval increased")]
    internal static partial void PollSlowed(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Device authorization rate storage is unavailable")]
    internal static partial void StorageUnavailable(this ILogger logger);
}
