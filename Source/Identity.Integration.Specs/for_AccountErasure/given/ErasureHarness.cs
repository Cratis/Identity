// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Erasing;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_NativeAccounts.given;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Integration.for_AccountErasure.given;

/// <summary>
/// Wires the real Chronicle-backed erasure log and key-destruction stage against the real kernel. The policy
/// hooks here are spec-local fixtures: they enable a run only inside these specs and are not production defaults.
/// </summary>
/// <param name="provider">The scoped host services.</param>
internal sealed class ErasureHarness(IServiceProvider provider)
{
    /// <summary>The boundary value used only by these specs; it is not an approved production boundary version.</summary>
    public static readonly BoundaryVersion SpecBoundary = new(1);

    /// <summary>The personal name appended before erasure; it must not be readable afterwards.</summary>
    public const string RenamedPersonalName = "Erasure Spec Renamed";

    /// <summary>Gets the account event store.</summary>
    public IEventStore Store { get; } = provider.GetRequiredService<IEventStore>();

    /// <summary>Gets the real log.</summary>
    public IAccountErasureLog Log => new ChronicleAccountErasureLog(Store, TimeProvider.System);

    /// <summary>Gets the requests service with a permissive spec-local authorization.</summary>
    public AccountErasureRequests Requests => new(Log, new AllowAll(), TimeProvider.System);

    /// <summary>Gets a coordinator that destroys keys for real and treats every other stage as already satisfied.</summary>
    public AccountErasureCoordinator Coordinator
    {
        get
        {
            var stages = ErasureStages.Required
                .Select(id => id == ErasureStages.DestroySubjectKeys
                    ? new DestroySubjectKeysStage(provider.GetRequiredService<IChronicleClient>(), Store, NullLogger<DestroySubjectKeysStage>.Instance)
                    : (IErasureStage)new Satisfied(id))
                .ToArray();
            return new(Log, stages, new Scope(), new Boundary(), TimeProvider.System);
        }
    }

    /// <summary>Registers an account with a personal name and tombstones it.</summary>
    /// <returns>The tombstoned account subject.</returns>
    public async Task<AccountSubject> TombstonedAccount()
    {
        var adapter = provider.GetRequiredService<EventSourcedAccountAdapter>();
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var connection = new GitHubConnection(GitHubConnection.FixedConnectionId, timeProvider);
        var user = new GitHubUser(UpstreamUserIds.Next(), "erasure-spec-login", "Erasure Spec Person");
        var account = await adapter.SignIn(connection.From(user, FactId.New()), new(null, user.Name, user.Login));
        (await Store.EventLog.Append(account.Id.Value, new AccountNameChanged(RenamedPersonalName))).IsSuccess.ShouldBeTrue();
        (await Store.EventLog.Append(account.Id.Value, new AccountTombstoneRecorded(timeProvider.GetUtcNow(), FactId.New()))).IsSuccess.ShouldBeTrue();
        return account.Id;
    }

    sealed class AllowAll : IErasureAuthorization
    {
        public Task<bool> IsAllowed(ClaimsPrincipal requester, AccountSubject account) => Task.FromResult(true);
    }

    sealed class Scope : IErasureTenantScope
    {
        public Task<IReadOnlyList<string>> NamespacesFor(AccountSubject account) => Task.FromResult<IReadOnlyList<string>>([EventStoreNamespaceName.Default.Value]);
    }

    sealed class Boundary : IErasureBoundaryPolicy
    {
        public BoundaryVersion? Approved => SpecBoundary;
    }

    sealed class Satisfied(ErasureStageId id) : IErasureStage
    {
        public ErasureStageId Id => id;

        public Task<ErasureStageOutcome> Execute(ErasureContext context) => Task.FromResult(ErasureStageOutcome.Success(context.Namespaces));
    }
}
