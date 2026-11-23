// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { parseCli, parseDiscovery, partitionCases, selectionArgs, run } from './run-integration-shard.mjs';

// Same six fields and types as native xUnit -list full/json, without real paths or case IDs.
const caseFor = (Class, ID) => ({ Assembly: '/build/specs.dll', DisplayName: `${Class}.${ID}`, ID, Class, Method: `should_${ID}`, Explicit: false });
const cases = [caseFor('Specs.A', 'a1'), caseFor('Specs.A', 'a2'), caseFor('Specs.B', 'b1'), caseFor('Specs.C', 'c1')];
const cli = ['--tfm', 'net10.0', '--shard', '0'];
const success = rows => ({ status: 0, signal: null, stdout: JSON.stringify(rows) });
const classes = rows => [...new Set(rows.map(row => row.Class))].sort();
const ids = rows => rows.map(row => row.ID).sort();

function fakeRunner(results = [success(cases), success(cases.slice(0, 2)), { status: 0 }]) {
    const calls = [];
    const messages = [];
    const errors = [];
    const launch = (...args) => {
        calls.push(args);
        const result = results[calls.length - 1];
        if (result instanceof Error) throw result;
        assert(result, 'Unexpected native launch');
        return result;
    };
    return { calls, messages, errors, launch, log: message => messages.push(message), error: message => errors.push(message) };
}

test('accepts exactly the supported TFM and two shard indexes', () => {
    for (const tfm of ['net10.0', 'net11.0']) {
        for (const shard of ['0', '1']) {
            assert.deepEqual(parseCli(['--shard', shard, '--tfm', tfm]), { tfm, shard: Number(shard) });
        }
    }
});

for (const args of [
    [], ['--tfm', 'net10.0'], ['--shard', '0'],
    ['--tfm', 'net9.0', '--shard', '0'],
    ...['-1', '2', '0.5', 'NaN', '', '00'].map(shard => ['--tfm', 'net10.0', '--shard', shard]),
    [...cli, '--other', 'value'], [...cli, 'extra'], [...cli, '--shard', '1'],
]) {
    test(`rejects invalid CLI before launching: ${JSON.stringify(args)}`, () => {
        const fake = fakeRunner();
        assert.throws(() => parseCli(args));
        assert.equal(run(args, fake), 2);
        assert.equal(fake.calls.length, 0);
    });
}

for (const text of ['not JSON', '{}', 'null', '[]', JSON.stringify(cases) + '\u001b[0m', '[null]', '[42]']) {
    test(`rejects invalid discovery envelope: ${JSON.stringify(text)}`, () => {
        assert.throws(() => parseDiscovery(text));
    });
}

for (const field of ['Assembly', 'DisplayName', 'ID', 'Class', 'Method', 'Explicit']) {
    test(`rejects missing ${field}`, () => {
        const row = caseFor('Specs.A', 'a1');
        delete row[field];
        assert.throws(() => parseDiscovery(JSON.stringify([row])));
    });
    for (const value of field === 'Explicit' ? [null, 0, 'false'] : [null, 0, '', ' ']) {
        test(`rejects invalid ${field}: ${JSON.stringify(value)}`, () => {
            assert.throws(() => parseDiscovery(JSON.stringify([{ ...cases[0], [field]: value }])));
        });
    }
}

for (const Class of ['Specs.*', 'Specs.?', '-class', 'Specs. A', 'Specs.A\nOther', 'Specs.A\u0000Other']) {
    test(`rejects unsafe exact class filter: ${JSON.stringify(Class)}`, () => {
        assert.throws(() => parseDiscovery(JSON.stringify([caseFor(Class, 'a1')])));
    });
}

test('rejects unknown schema fields and duplicate identities, even across classes', () => {
    assert.throws(() => parseDiscovery(JSON.stringify([{ ...cases[0], Unexpected: true }])));
    assert.throws(() => parseDiscovery(JSON.stringify([cases[0], cases[0]])));
    assert.throws(() => parseDiscovery(JSON.stringify([cases[0], caseFor('Specs.B', 'a1')])));
});

test('preserves valid discovery including explicit metadata', () => {
    const rows = [cases[0], { ...cases[1], Explicit: true }];
    assert.deepEqual(parseDiscovery(JSON.stringify(rows)), rows);
});

test('count-weighted partition is deterministic, whole-class, disjoint and exhaustive', () => {
    const rows = [
        ...Array.from({ length: 4 }, (_, i) => caseFor('Specs.Z', `z${i}`)),
        ...Array.from({ length: 3 }, (_, i) => caseFor('Specs.B', `b${i}`)),
        caseFor('Specs.a', 'a1'), caseFor('Specs.C', 'c1'), caseFor('Specs.D', 'd1'),
    ];
    const shards = partitionCases(rows);
    // Largest first; ordinal B before C before D before lowercase a; equal loads choose shard 0.
    assert.deepEqual(shards.map(classes), [['Specs.D', 'Specs.Z'], ['Specs.B', 'Specs.C', 'Specs.a']]);
    assert.deepEqual(shards.map(shard => shard.length), [5, 5]);
    assert.deepEqual(partitionCases([...rows].reverse()), shards);
    assert.deepEqual(partitionCases([...rows.slice(4), ...rows.slice(0, 4)]), shards);
    assert.deepEqual(ids(shards.flat()), ids(rows));
    assert.equal(new Set(shards.flat().map(row => row.ID)).size, rows.length);
    assert.equal(classes(shards[0]).some(name => classes(shards[1]).includes(name)), false);
});

test('ordinal class ties and equal loads favor the lower shard index', () => {
    const rows = ['Specs.b', 'Specs.B', 'Specs.A', 'Specs.a'].map((name, i) => caseFor(name, `case${i}`));
    assert.deepEqual(partitionCases(rows).map(classes), [['Specs.A', 'Specs.a'], ['Specs.B', 'Specs.b']]);
});

test('refuses to create an empty shard', () => {
    assert.throws(() => partitionCases([]));
    assert.throws(() => partitionCases([cases[0]]));
    assert.throws(() => partitionCases(cases.slice(0, 2)));
});

test('discovery and run retain notrait even for a class with excluded conformance cases', () => {
    const selected = ['Specs.Mixed', 'Specs.Other'];
    const filters = ['-notrait', 'Category=Conformance', '-class', 'Specs.Mixed', '-class', 'Specs.Other'];
    assert.deepEqual(selectionArgs(selected, true), [...filters, '-list', 'full/json', '-noColor']);
    assert.deepEqual(selectionArgs(selected, false), [...filters, '-noAutoReporters', '-reporter', 'identity-lifecycle']);
    assert.deepEqual(selectionArgs([], true), ['-notrait', 'Category=Conformance', '-list', 'full/json', '-noColor']);
});

test('preflights the exact selected IDs before launching native execution once', () => {
    const fake = fakeRunner([success(cases), success([cases[1], cases[0]]), { status: 0 }]);
    assert.equal(run(cli, fake), 0);
    assert.equal(fake.calls.length, 3);
    const [discovery, preflight, execution] = fake.calls;
    assert.match(discovery[0], /Source\/Identity\.Integration\.Specs\/bin\/Release\/net10\.0\/Cratis\.Identity\.Integration\.Specs$/);
    assert.equal(preflight[0], discovery[0]);
    assert.equal(execution[0], discovery[0]);
    assert.deepEqual(discovery[1], selectionArgs([], true));
    assert.deepEqual(preflight[1], selectionArgs(['Specs.A'], true));
    assert.deepEqual(execution[1], selectionArgs(['Specs.A'], false));
    assert.deepEqual(discovery[2].stdio, ['ignore', 'pipe', 'inherit']);
    assert.deepEqual(preflight[2], discovery[2]);
    assert.equal(discovery[2].encoding, 'utf8');
    assert.equal(discovery[2].maxBuffer, 16 * 1024 * 1024);
    assert.deepEqual(execution[2], { stdio: 'inherit' });
    assert.equal(fake.messages.length, 1);
    assert.match(fake.messages[0], /shard 0\/2: 1\/3 classes, 2\/4 discovery cases/);
    assert.deepEqual(fake.errors, []);
});

test('shard one selects every remaining class and resolves its own TFM', () => {
    const fake = fakeRunner([success(cases), success(cases.slice(2)), { status: 0 }]);
    assert.equal(run(['--tfm', 'net11.0', '--shard', '1'], fake), 0);
    assert.match(fake.calls[2][0], /net11\.0\/Cratis\.Identity\.Integration\.Specs$/);
    assert.deepEqual(fake.calls[2][1], selectionArgs(['Specs.B', 'Specs.C'], false));
});

for (const selected of [
    [cases[0]], [cases[0], cases[1], cases[2]], [cases[0], cases[0]],
    [{ ...cases[0], Class: 'Specs.Wrong' }, cases[1]],
    [{ ...cases[0], ID: 'wrong' }, cases[1]], [],
]) {
    test(`mismatched preflight blocks execution: ${JSON.stringify(ids(selected))}`, () => {
        const fake = fakeRunner([success(cases), success(selected)]);
        assert.equal(run(cli, fake), 2);
        assert.equal(fake.calls.length, 2);
        assert.equal(fake.errors.length, 1);
    });
}

const failures = [
    { status: 1, stdout: JSON.stringify(cases) },
    { status: 7, stdout: JSON.stringify(cases) },
    { status: null, signal: 'SIGTERM', stdout: JSON.stringify(cases) },
    { status: null, stdout: JSON.stringify(cases) },
    { status: 0, error: new Error('spawn failed'), stdout: JSON.stringify(cases) },
    { status: 0, error: Object.assign(new Error('buffer overflow'), { code: 'ENOBUFS' }), stdout: JSON.stringify(cases) },
    { status: 0, stdout: 'malformed output' },
    { status: 0, stdout: JSON.stringify([cases[0]]) },
    new Error('launcher threw'),
];
for (const [index, failure] of failures.entries()) {
    for (const phase of [0, 1]) {
        test(`producer failure ${index} in discovery phase ${phase} cannot execute tests`, () => {
            const fake = fakeRunner(phase === 0 ? [failure] : [success(cases), failure]);
            assert.equal(run(cli, fake), 2);
            assert.equal(fake.calls.length, phase + 1);
            assert.equal(fake.errors.length, 1);
        });
    }
}

for (const status of [0, 1, 7, 255]) {
    test(`propagates executed native status ${status}`, () => {
        const fake = fakeRunner([success(cases), success(cases.slice(0, 2)), { status }]);
        assert.equal(run(cli, fake), status);
        assert.equal(fake.calls.length, 3);
    });
}

for (const result of [{ status: null }, { status: null, signal: 'SIGKILL' }, { status: 0, error: new Error('spawn failed') }, new Error('launch failed')]) {
    test(`execution signal/error is nonzero: ${String(result)}`, () => {
        const fake = fakeRunner([success(cases), success(cases.slice(0, 2)), result]);
        assert.equal(run(cli, fake), 2);
        assert.equal(fake.calls.length, 3);
        assert.equal(fake.errors.length, 1);
    });
}
