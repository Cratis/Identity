// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Hosting;

internal static partial class DisabledResourcesNoticeLogging
{
    [LoggerMessage(LogLevel.Information, "Optional resource {Audience} is not configured and is disabled; it is not advertised and issues no tokens.")]
    internal static partial void OptionalResourceDisabled(this ILogger<DisabledResourcesNotice> logger, string audience);
}
