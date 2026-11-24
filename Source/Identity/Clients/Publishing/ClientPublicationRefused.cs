// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients.Publishing;

/// <summary>A client contract could not be published; it holds the source partition and never skips a fact.</summary>
/// <param name="reason">The reason code, never credential or personal content.</param>
internal class ClientPublicationRefused(string reason) : Exception(reason);
