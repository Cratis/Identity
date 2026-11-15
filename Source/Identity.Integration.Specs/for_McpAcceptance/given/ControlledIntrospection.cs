// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_McpAcceptance.given;

/// <summary>
/// How the introspection call behaves for a spec.
/// </summary>
public enum IntrospectionBehavior
{
    /// <summary>Forward to Identity.</summary>
    Forward = 0,

    /// <summary>Fail the connection.</summary>
    Unreachable = 1,

    /// <summary>Answer 500.</summary>
    ServerError = 2,

    /// <summary>Answer 200 with a body that is not JSON.</summary>
    Garbage = 3,

    /// <summary>Answer 200 with JSON that has no <c language="text">active</c> member.</summary>
    MissingActive = 4
}

/// <summary>
/// Sits in front of Identity's introspection endpoint, counts the calls that reach it and can make it fail.
/// </summary>
public sealed class ControlledIntrospection : DelegatingHandler
{
    /// <summary>
    /// Gets the number of introspection calls made.
    /// </summary>
    public int Calls { get; private set; }

    /// <summary>
    /// Gets or sets how calls are answered.
    /// </summary>
    public IntrospectionBehavior Behavior { get; set; } = IntrospectionBehavior.Forward;

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return Behavior switch
        {
            IntrospectionBehavior.Unreachable => throw new HttpRequestException("Identity is unreachable."),
            IntrospectionBehavior.ServerError => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            IntrospectionBehavior.Garbage => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>not json</html>") }),
            IntrospectionBehavior.MissingActive => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { scope = "direct:mcp:read" }) }),
            _ => base.SendAsync(request, cancellationToken)
        };
    }
}
