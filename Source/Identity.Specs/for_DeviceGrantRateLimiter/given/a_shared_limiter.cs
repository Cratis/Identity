// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Device;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_DeviceGrantRateLimiter.given;

public class a_shared_limiter : Specification
{
    private protected operational_windows _store;
    private protected capturing_logger _logger;
    private protected DeviceGrantRateLimiter _first;
    private protected DeviceGrantRateLimiter _second;
    private protected IdentityOptions _options;
    protected controlled_clock _clock;
    protected bool[] _results;

    void Establish()
    {
        _store = new();
        _clock = new();
        _options = new();
        _logger = new();
        _first = new(_store, Options.Create(_options), _clock, _logger);
        _second = new(_store, Options.Create(_options), _clock, _logger);
    }

    protected Task<bool> Verify(string code = "BCDFGHJK", string account = "account", string ip = "192.0.2.1") => _first.Verify(code, account, ip, CancellationToken.None);
}
