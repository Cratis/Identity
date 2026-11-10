// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// Names one separately registered MCP resource, independently of any token's claims.
/// </summary>
/// <param name="Indicator">The RFC 8707 resource indicator.</param>
/// <param name="Audience">The expected access-token audience.</param>
/// <param name="Scope">The registered MCP read scope.</param>
public record McpProduct(string Indicator, string Audience, string Scope);
