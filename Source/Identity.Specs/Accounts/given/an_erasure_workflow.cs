// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.given;

public class an_erasure_workflow : Specification
{
    private protected AccountSubject _account;
    private protected ErasureId _erasure;
    private protected InMemoryErasureLog _log;
    private protected List<string> _executed;
    private protected FakeStage[] _stages;
    private protected bool _authorized;
    private protected bool _tombstoned;
    private protected BoundaryVersion? _boundary;
    private protected IReadOnlyList<string> _namespaces;

    private protected AccountErasureRequests Requests => new(_log, new FakeAuthorization(_authorized), TimeProvider.System);

    private protected AccountErasureCoordinator Coordinator => new(_log, _stages, new FakeScope(_namespaces), new FakeBoundary(_boundary), TimeProvider.System);

    void Establish()
    {
        _account = AccountSubject.New();
        _erasure = ErasureId.New();
        _log = new();
        _executed = [];
        _authorized = true;
        _tombstoned = true;
        _boundary = new BoundaryVersion(1);
        _namespaces = ["tenant-a", "tenant-b"];
        _stages = ErasureStages.Required.Select(id => new FakeStage(id, _executed)).ToArray();
    }

    private protected Task Tombstone() => _log.Append(_account, new AccountTombstoneRecorded(DateTimeOffset.UnixEpoch, FactId.New()));

    private protected static async Task<string?> Refusal(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (AccountOperationRefused exception)
        {
            return exception.Message;
        }
    }

    private protected class InMemoryErasureLog : IAccountErasureLog
    {
        public List<object> Facts { get; } = [];

        public Task<ErasureState> Read(AccountSubject account) => Task.FromResult(ErasureState.Fold(Facts));

        public Task Append(AccountSubject account, object fact)
        {
            Facts.Add(fact);
            return Task.CompletedTask;
        }
    }

    private protected class FakeStage(ErasureStageId id, List<string> executed) : IErasureStage
    {
        public ErasureStageOutcome? Outcome { get; set; }

        public IReadOnlyList<string>? Covered { get; set; }

        public Exception? Throws { get; set; }

        public ErasureStageId Id => id;

        public Task<ErasureStageOutcome> Execute(ErasureContext context)
        {
            executed.Add(id.Value);
            return Throws is null ? Task.FromResult(Outcome ?? ErasureStageOutcome.Success(Covered ?? context.Namespaces)) : Task.FromException<ErasureStageOutcome>(Throws);
        }
    }

    sealed class FakeAuthorization(bool allowed) : IErasureAuthorization
    {
        public Task<bool> IsAllowed(ClaimsPrincipal requester, AccountSubject account) => Task.FromResult(allowed);
    }

    sealed class FakeScope(IReadOnlyList<string> namespaces) : IErasureTenantScope
    {
        public Task<IReadOnlyList<string>> NamespacesFor(AccountSubject account) => Task.FromResult(namespaces);
    }

    sealed class FakeBoundary(BoundaryVersion? approved) : IErasureBoundaryPolicy
    {
        public BoundaryVersion? Approved => approved;
    }
}
