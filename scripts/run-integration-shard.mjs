// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Temporary whole-class containment pending integration phase profiling and the producer fix.
// Usage: node scripts/run-integration-shard.mjs --tfm net10.0 --shard 0

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { parseArgs } from 'node:util';

const root = fileURLToPath(new URL('../', import.meta.url));
const ordinal = (left, right) => left < right ? -1 : left > right ? 1 : 0;
const fields = ['Assembly', 'Class', 'DisplayName', 'Explicit', 'ID', 'Method'];
const textFields = fields.filter(field => field !== 'Explicit');

export function parseCli(args) {
    const { values, tokens } = parseArgs({
        args, strict: true, allowPositionals: false, tokens: true,
        options: { tfm: { type: 'string' }, shard: { type: 'string' } },
    });
    assert.equal(tokens.length, 2, 'Specify --tfm and --shard exactly once');
    assert(['net10.0', 'net11.0'].includes(values.tfm), 'Unsupported or missing --tfm');
    assert(['0', '1'].includes(values.shard), '--shard must be 0 or 1');
    return { tfm: values.tfm, shard: Number(values.shard) };
}

export function parseDiscovery(stdout) {
    const rows = JSON.parse(stdout);
    assert(Array.isArray(rows) && rows.length > 0, 'Native discovery must be a nonempty array');
    const ids = new Set();
    for (const row of rows) {
        assert(row && typeof row === 'object' && !Array.isArray(row), 'Invalid discovery record');
        assert.deepEqual(Object.keys(row).sort(), fields, 'Unknown discovery record schema');
        for (const field of textFields) {
            assert(typeof row[field] === 'string' && row[field].trim().length > 0, `Invalid discovery ${field}`);
        }
        assert.equal(typeof row.Explicit, 'boolean', 'Invalid discovery Explicit');
        assert(!/[*?\s\p{Cc}]/u.test(row.Class) && !row.Class.startsWith('-'), 'Class cannot be safely selected by an exact native filter');
        assert(!ids.has(row.ID), 'Duplicate discovery ID');
        ids.add(row.ID);
    }
    return rows;
}

function assertSelection(expected, actual) {
    const identities = rows => rows.map(row => [row.ID, row.Class]).sort(([left], [right]) => ordinal(left, right));
    assert.deepEqual(identities(actual), identities(expected), 'Selected discovery must match canonical IDs and classes exactly');
}

const classNames = rows => [...new Set(rows.map(row => row.Class))].sort(ordinal);

export function partitionCases(rows) {
    const groups = new Map();
    for (const row of rows) {
        if (!groups.has(row.Class)) groups.set(row.Class, []);
        groups.get(row.Class).push(row);
    }
    assert(groups.size >= 2, 'Need at least two classes for two nonempty shards');
    const ordered = [...groups].sort(([left, a], [right, b]) => b.length - a.length || ordinal(left, right));
    const shards = [[], []];
    for (const [, group] of ordered) {
        const index = shards[0].length <= shards[1].length ? 0 : 1;
        shards[index].push(...[...group].sort((a, b) => ordinal(a.ID, b.ID)));
    }
    assert(shards.every(shard => shard.length > 0), 'Empty shard');
    assertSelection(rows, shards.flat());
    assert.equal(new Set(shards.flat().map(row => row.ID)).size, rows.length, 'Overlapping case IDs');
    const firstClasses = new Set(classNames(shards[0]));
    assert(classNames(shards[1]).every(name => !firstClasses.has(name)), 'Overlapping classes');
    return shards;
}

export function selectionArgs(classes, discovery) {
    const filters = ['-notrait', 'Category=Conformance', ...classes.flatMap(name => ['-class', name])];
    return [...filters, ...(discovery ? ['-list', 'full/json', '-noColor'] : ['-noAutoReporters', '-reporter', 'identity-lifecycle'])];
}

function childStatus(result) {
    if (result.error) throw result.error;
    assert(!result.signal, `Native process terminated by ${result.signal}`);
    assert(Number.isInteger(result.status) && result.status >= 0 && result.status <= 255, 'Native process returned no valid exit status');
    return result.status;
}

function discover(executable, classes, launch) {
    const result = launch(executable, selectionArgs(classes, true), {
        encoding: 'utf8', stdio: ['ignore', 'pipe', 'inherit'], maxBuffer: 16 * 1024 * 1024,
    });
    assert.equal(childStatus(result), 0, 'Native discovery failed');
    return parseDiscovery(result.stdout);
}

export function run(args, { launch = spawnSync, log = console.log, error = console.error } = {}) {
    try {
        const { tfm, shard } = parseCli(args);
        const executable = resolve(root, 'Source/Identity.Integration.Specs/bin/Release', tfm, 'Cratis.Identity.Integration.Specs');
        const all = discover(executable, [], launch);
        const selected = partitionCases(all)[shard];
        const classes = classNames(selected);
        const preflight = discover(executable, classes, launch);
        assertSelection(selected, preflight);
        log(`Integration shard ${shard}/2: ${classes.length}/${classNames(all).length} classes, ${selected.length}/${all.length} discovery cases (${tfm}).`);
        return childStatus(launch(executable, selectionArgs(classes, false), { stdio: 'inherit' }));
    } catch (failure) {
        error(`Integration shard could not run: ${failure.message}`);
        return 2;
    }
}

if (process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url) {
    process.exitCode = run(process.argv.slice(2));
}
