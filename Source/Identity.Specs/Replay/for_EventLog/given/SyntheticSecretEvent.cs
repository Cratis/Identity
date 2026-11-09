// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Identity.Replay.for_EventLog.given;

internal sealed record SyntheticSecretEvent(string AccessToken, [property: JsonPropertyName("client_secret")] string ClientSecret);
