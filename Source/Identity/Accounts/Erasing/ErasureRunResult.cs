// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The outcome of one coordinator run.</summary>
/// <param name="Completed">Whether the erasure is complete.</param>
/// <param name="FailedStage">The stage that failed this run, if any.</param>
internal record ErasureRunResult(bool Completed, ErasureStageId? FailedStage);
