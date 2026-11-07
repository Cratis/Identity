// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Identity.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Test-only pause points in the refresh-token sign-in pipeline, armed per refresh token so other specs are unaffected.
/// </summary>
/// <remarks>
/// The barrier sits just before <see cref="RedeemRefreshTokenOnce"/>: every request that reaches it has passed
/// token validation. The before-redemption hold precedes the atomic consume/install write; the existing hold
/// follows that write and OpenIddict's redemption, before any successor token is generated.
/// </remarks>
public class RefreshGates
{
    static readonly TimeSpan _waitLimit = TimeSpan.FromSeconds(30);

    readonly ConcurrentDictionary<string, Rendezvous> _barriers = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, Hold> _holds = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, Hold> _beforeRedemptionHolds = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the descriptor of the barrier handler.
    /// </summary>
    public OpenIddictServerHandlerDescriptor BarrierDescriptor => OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseInlineHandler(async context =>
        {
            if (RefreshToken(context) is { } token && _barriers.TryGetValue(token, out var barrier))
            {
                await barrier.Arrive();
            }
        })
        .SetOrder(RedeemRefreshTokenOnce.Descriptor.Order - 1)
        .SetType(OpenIddictServerHandlerType.Custom)
        .Build();

    /// <summary>
    /// Gets the descriptor of the hold before the atomic consume/install write.
    /// </summary>
    public OpenIddictServerHandlerDescriptor BeforeRedemptionHoldDescriptor => OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseInlineHandler(async context =>
        {
            if (RefreshToken(context) is { } token && _beforeRedemptionHolds.TryRemove(token, out var hold))
            {
                await hold.Wait();
            }
        })
        .SetOrder(RedeemRefreshTokenOnce.Descriptor.Order - 1)
        .SetType(OpenIddictServerHandlerType.Custom)
        .Build();

    /// <summary>
    /// Gets the descriptor of the hold after the atomic consume/install write.
    /// </summary>
    public OpenIddictServerHandlerDescriptor HoldDescriptor => OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseInlineHandler(async context =>
        {
            if (RefreshToken(context) is { } token && _holds.TryRemove(token, out var hold))
            {
                await hold.Wait();
            }
        })
        .SetOrder(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order + 1)
        .SetType(OpenIddictServerHandlerType.Custom)
        .Build();

    /// <summary>
    /// Makes every refresh with the token wait until the given number of refreshes with it have passed validation.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="participants">The number of requests to wait for.</param>
    /// <returns>A <see cref="IDisposable"/> that disarms the barrier.</returns>
    public IDisposable ArmBarrier(string refreshToken, int participants)
    {
        _barriers[refreshToken] = new Rendezvous(participants);
        return new Disarm(() => _barriers.TryRemove(refreshToken, out _));
    }

    /// <summary>
    /// Holds the next validated refresh before the atomic consume/install write.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>The <see cref="Hold"/>.</returns>
    public Hold ArmBeforeRedemptionHold(string refreshToken)
    {
        var hold = new Hold();
        _beforeRedemptionHolds[refreshToken] = hold;
        return hold;
    }

    /// <summary>
    /// Holds the next refresh after the atomic consume/install write and redemption, before successor generation.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>The <see cref="Hold"/>.</returns>
    public Hold ArmHold(string refreshToken)
    {
        var hold = new Hold();
        _holds[refreshToken] = hold;
        return hold;
    }

    static string? RefreshToken(ProcessSignInContext context) =>
        context.EndpointType is OpenIddictServerEndpointType.Token && context.Request?.IsRefreshTokenGrantType() == true
            ? context.Request.RefreshToken
            : null;

    /// <summary>
    /// A request paused until released.
    /// </summary>
    public sealed class Hold
    {
        readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets a task that completes once the held request has reached the hold.
        /// </summary>
        public Task Reached => _reached.Task.WaitAsync(_waitLimit);

        /// <summary>
        /// Lets the held request continue.
        /// </summary>
        public void Release() => _released.TrySetResult();

        internal Task Wait()
        {
            _reached.TrySetResult();
            return _released.Task.WaitAsync(_waitLimit);
        }
    }

    sealed class Rendezvous(int participants)
    {
        readonly TaskCompletionSource _all = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _arrived;

        public Task Arrive()
        {
            if (Interlocked.Increment(ref _arrived) >= participants)
            {
                _all.TrySetResult();
            }

            return _all.Task.WaitAsync(_waitLimit);
        }
    }

    sealed class Disarm(Action disarm) : IDisposable
    {
        public void Dispose() => disarm();
    }
}
