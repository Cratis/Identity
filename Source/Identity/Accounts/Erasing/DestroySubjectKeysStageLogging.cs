// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// Log messages for <see cref="DestroySubjectKeysStage"/>. Only the namespace and the exception type are logged: never the subject or the exception message.
/// </summary>
internal static partial class DestroySubjectKeysStageLogging
{
    [LoggerMessage(LogLevel.Error, "Subject key destruction failed in namespace {Namespace}: {ExceptionType}")]
    internal static partial void KeyDestructionFailed(this ILogger<DestroySubjectKeysStage> logger, string @namespace, string exceptionType);
}
