// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow.Deciding.given;

public class an_activation : Specification
{
    protected readonly MemoryRequests store = new();
    protected readonly MemoryEvidence evidence = new();
    protected readonly MemoryFence fence = new();
    protected readonly MemoryAuthorizations authorizations = new();
    protected readonly ManualClock clock = new();
    protected readonly ConsentActivationOptions options = new();
    protected readonly ConsentBinding binding = new("realm-a", "00000000-0000-4000-8000-000000000001", "client-a", "tenant-a", "resource-a", "openid read", "session-a", new AuthorizationRequestId(Guid.Parse("00000000-0000-4000-8000-000000000003")));
    protected ConsentActivation activation;
    protected ConsentRequest request;

    async Task Establish()
    {
        store.Clock = clock;
        activation = Restart();
        request = await activation.Create(binding, CancellationToken.None);
    }

    protected ConsentActivation Restart() => new(store, evidence, fence, authorizations, clock, Options.Create(options));

    protected Task<ConsentRequest?> Approve() => activation.Decide(request.Id, binding, true, CancellationToken.None);
    protected Task<ConsentRequest?> Resume() => activation.Resume(request.Id, CancellationToken.None);
    protected Task<string?> Claim() => activation.ClaimCode(request.Id, binding, CancellationToken.None);
    protected void Expire() => clock.Now += options.ActivationDeadline;

    public class ManualClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
