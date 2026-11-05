// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts;

namespace Cratis.Identity.Clients.for_ClientContracts;

public record UnsafeClientPayload(string ClientSecret, string RedirectUri, SecretDigest Secret);
