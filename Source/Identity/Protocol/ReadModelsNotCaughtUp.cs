// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Protocol;

/// <summary>The protocol must not acknowledge facts before the required read models catch up.</summary>
public sealed class ReadModelsNotCaughtUp() : Exception("read_models_not_caught_up");
