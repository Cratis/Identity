// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts.for_AccountContracts;

public record UnsafePayload(string Email, string Login, string IdpSub, string PasswordHash, string RefreshToken);
