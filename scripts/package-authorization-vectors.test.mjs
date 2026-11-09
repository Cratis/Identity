// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, readdir } from 'node:fs/promises';
import { test } from 'node:test';
import { gunzipSync } from 'node:zlib';
import { createArchive, validateBaseline, validateBaselineHistory, validateCases } from './package-authorization-vectors.mjs';

const directory = new URL('../Source/Identity.Specs/Vectors/arc-mcp-authorization-v1/', import.meta.url);
const manifest = JSON.parse(await readFile(new URL('manifest.json', directory)));
const baseline = JSON.parse(await readFile(new URL(`baselines/v${manifest.version}.json`, directory)));
const changelog = await readFile(new URL('CHANGELOG.md', directory), 'utf8');
const baselineNames = await readdir(new URL('baselines/', directory));
const hash = bytes => createHash('sha256').update(bytes).digest('hex');

test('archive bytes do not depend on input order or wall clock', () => {
    const entries = [['z.json', Buffer.from('{}')], ['a.json', Buffer.from('[]')]];
    assert.deepEqual(createArchive(entries), createArchive([...entries].reverse()));
});

test('archive contains sorted files, fixed metadata, payload bytes and end markers', () => {
    const archive = createArchive([['z.json', Buffer.from('{}')], ['a.json', Buffer.from('[]')]]);
    const tar = gunzipSync(archive);
    assert.equal(tar.subarray(0, 6).toString(), 'a.json');
    assert.equal(tar.subarray(512, 514).toString(), '[]');
    assert.equal(tar.subarray(1024, 1030).toString(), 'z.json');
    assert.equal(tar.subarray(1536, 1538).toString(), '{}');
    assert.equal(tar.subarray(100, 108).toString(), '0000644\0');
    assert.equal(tar.subarray(108, 116).toString(), '0000000\0');
    assert.equal(tar.subarray(116, 124).toString(), '0000000\0');
    assert.equal(tar.subarray(136, 148).toString(), '00000000000\0');
    assert.equal(tar.subarray(257, 265).toString(), 'ustar\0' + '00');
    assert.deepEqual(tar.subarray(2048), Buffer.alloc(1024));
    assert.deepEqual(archive.subarray(4, 8), Buffer.alloc(4));
    assert.equal(archive[9], 255);
});

test('archive refuses names that would be truncated', () => {
    assert.throws(() => createArchive([['a'.repeat(101), Buffer.alloc(0)]]), /ustar name field/);
});

test('real manifest matches the independent versioned baseline', () => {
    validateBaseline(manifest, baseline);
    validateBaselineHistory(manifest, baselineNames, changelog);
    assert.equal(Object.keys(baseline.cases).length, manifest.cases.length);
});

test('baseline rejects swapped ids even though case hashes are unchanged', () => {
    const changed = structuredClone(manifest);
    [changed.cases[0].id, changed.cases[1].id] = [changed.cases[1].id, changed.cases[0].id];
    assert.throws(() => validateBaseline(changed, baseline), /Released case bindings/);
});

test('baseline rejects a fresh valid kebab-case id', () => {
    const changed = structuredClone(manifest);
    changed.cases[0].id = 'fresh-kebab-case-id';
    assert.throws(() => validateBaseline(changed, baseline), /Released case bindings/);
});

test('baseline rejects coordinated vector and manifest hash edits without a bump', async () => {
    const changed = structuredClone(manifest);
    const vectors = JSON.parse(await readFile(new URL(changed.files[0].path, directory)));
    vectors.given.vectors[0].baselineMutation = true;
    const bytes = Buffer.from(JSON.stringify(vectors));
    changed.cases[0].sha256 = hash(JSON.stringify(vectors.given.vectors[0]));
    changed.files[0].sha256 = hash(bytes);
    validateCases(changed, [[changed.files[0].path, bytes]]);
    assert.throws(() => validateBaseline(changed, baseline), /Released case bindings/);
});

test('baseline rejects a vector file hash edit alone', () => {
    const changed = structuredClone(manifest);
    changed.files[0].sha256 = '0'.repeat(64);
    assert.throws(() => validateBaseline(changed, baseline), /Released file hashes/);
});

test('a version bump requires a new baseline and changelog entry', () => {
    const changed = { ...manifest, version: '1.1.0' };
    const updatedChangelog = changelog.replace('## 1.0.0', '## 1.1.0\n\n## 1.0.0');
    assert.throws(() => validateBaseline(changed, baseline), /Baseline version mismatch/);
    assert.throws(() => validateBaselineHistory(changed, ['v1.0.0.json'], updatedChangelog), /Missing current baseline/);
    assert.throws(() => validateBaselineHistory(changed, ['v1.0.0.json', 'v1.1.0.json'], changelog), /Changelog version mismatch/);
    validateBaselineHistory(changed, ['v1.0.0.json', 'v1.1.0.json'], updatedChangelog);
});

test('every older baseline must be retained and listed in the changelog', () => {
    const changed = { ...manifest, version: '1.2.0' };
    const history = '## 1.2.0\n## 1.1.0\n## 1.0.0';
    assert.throws(() => validateBaselineHistory(changed, ['v1.2.0.json', 'v1.0.0.json'], history), /retain its baseline/);
    assert.throws(() => validateBaselineHistory(changed, ['v1.2.0.json', 'v1.1.0.json', 'v1.0.0.json'], '## 1.2.0\n## 1.0.0'), /retain its baseline/);
    assert.throws(() => validateBaselineHistory(changed, ['v1.2.0.json'], '## 1.2.0'), /Missing initial baseline/);
});
