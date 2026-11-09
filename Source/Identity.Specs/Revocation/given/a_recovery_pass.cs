// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs.given;

public class a_recovery_pass : Specification
{
    private protected IRecoveryLog _log;
    private protected IRecoveryStore _operational;
    private protected IReservationAuthority _authority;
    private protected IRecoveryFactWriter _facts;
    private protected IRevocationFactWriter _restrictionFacts;
    private protected DispatchRevocationOutbox _dispatch;
    private protected IFenceStore _fences;
    private protected RecoveryGate _gate;
    private protected RecoverySession _session;
    private protected FenceLogOptions _options;
    private protected ReconcileRecovery _recovery;
    private protected RecoveryCustody _custody;
    private protected RecoveryCheckpoint? _checkpoint;
    private protected FenceRecord _record;
    protected bool _contains;
    protected long _allocated;
    protected long _watermark;
    private protected List<RecoveryEpochIntent> _epochs;
    private protected List<(FenceRecord Record, bool Reset)> _applied;
    protected Exception? _failure;

    void Establish()
    {
        _log = Substitute.For<IRecoveryLog>();
        _operational = Substitute.For<IRecoveryStore>();
        _authority = Substitute.For<IReservationAuthority>();
        _facts = Substitute.For<IRecoveryFactWriter>();
        _fences = Substitute.For<IFenceStore>();
        _gate = new();
        _session = new() { Bound = true };
        _options = new() { InstanceId = "test-custody" };
        _custody = new("test-custody", 1, "original", 0, 0);
        _checkpoint = new("test-custody", 1, "original");
        var fence = new GrantFence(Guid.NewGuid(), 1, "grant", Guid.NewGuid().ToString("D"), RevocationReason.RefreshTokenReused, DateTimeOffset.UnixEpoch);
        var entries = FenceRecordFactory.Entries(fence);
        _record = new(fence, entries.Select(entry => entry.Id).ToArray(), entries);
        _contains = true;
        _allocated = _watermark = 1;
        _epochs = [];
        _applied = [];
        _log.Custody(Arg.Any<CancellationToken>()).Returns(_ => _custody);
        _log.Acquire(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _log.Read(0, Arg.Any<CancellationToken>()).Returns(_ => [_record]);
        _log.Watermark(Arg.Any<CancellationToken>()).Returns(_ => _watermark);
        _log.AllocatedThrough(Arg.Any<CancellationToken>()).Returns(_ => _allocated);
        _log.Checkpoint(Arg.Any<string>(), Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var checkpoint = call.Arg<RecoveryCheckpoint>();
            _custody = _custody with { AppliedThrough = checkpoint.AppliedThrough, Nonce = checkpoint.Nonce };
            return Task.CompletedTask;
        });
        _log.RegisterRestore(Arg.Any<string>(), Arg.Any<RestoreKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _custody = _custody with { Epoch = _custody.Epoch + 1 };
            _watermark = ++_allocated;
            _epochs.Add(new(call.ArgAt<string>(0), _custody.Epoch, DateTimeOffset.UnixEpoch, Guid.NewGuid(), _allocated));
            return _custody.Epoch;
        });
        _log.EpochIntents(Arg.Any<CancellationToken>()).Returns(_ => _epochs);
        _log.CompleteRestore(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _custody = _custody with { ClearedEpoch = call.Arg<bool>() ? _custody.Epoch : -1 };
            return Task.CompletedTask;
        });
        _operational.Checkpoint(Arg.Any<CancellationToken>()).Returns(_ => _checkpoint);
        _operational.Contains(Arg.Any<FenceRecord>(), Arg.Any<CancellationToken>()).Returns(_ => _contains);
        _operational.Apply(Arg.Any<FenceRecord>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _applied.Add((call.Arg<FenceRecord>(), call.Arg<bool>()));
            _contains = true;
            return Task.CompletedTask;
        });
        _operational.Save(Arg.Any<RecoveryCheckpoint?>(), Arg.Any<RecoveryCheckpoint>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (_checkpoint != call.ArgAt<RecoveryCheckpoint?>(0))
            {
                throw new RevocationPending();
            }

            _checkpoint = call.ArgAt<RecoveryCheckpoint>(1);
            return Task.CompletedTask;
        });
        _fences.Undelivered(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(_ => []);
        _restrictionFacts = Substitute.For<IRevocationFactWriter>();
        _dispatch = new(_fences, new ConfirmFenceLog(_fences, Substitute.For<IFenceLog>()), _restrictionFacts);
        _recovery = new(_log, _operational, _authority, new ReconcileReservation(_log, _authority), _facts, _dispatch, _gate, _session, _options, TimeProvider.System);
    }

    protected async Task Run() => _failure = await Catch.Exception(() => _recovery.Run(CancellationToken.None));
}
