// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, readdir } from 'node:fs/promises';
import { test } from 'node:test';
import { gunzipSync } from 'node:zlib';
import { caseBindings, createArchive, loadArtifact, overlayDirectory, validateBaseline, validateBaselineHistory, validateCases, validateInheritance, vectorDirectory } from './package-authorization-vectors.mjs';

const directory = new URL('../Source/Identity.Specs/Vectors/arc-mcp-authorization-v1/', import.meta.url);
const manifest = JSON.parse(await readFile(new URL('manifest.json', directory)));
const baseline = JSON.parse(await readFile(new URL(`baselines/v${manifest.version}.json`, directory)));
const changelog = await readFile(new URL('CHANGELOG.md', directory), 'utf8');
const baselineNames = await readdir(new URL('baselines/', directory));
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const overlayBytes = await readFile(new URL('../Source/Identity.Specs/Vectors/v2.1.0/manifest.json', import.meta.url));
const overlay = JSON.parse(overlayBytes);
const overlayBaseline = JSON.parse(await readFile(new URL('../Source/Identity.Specs/Vectors/v2.1.0/baselines/v2.1.0.json', import.meta.url)));

test('both immutable 2.0.0 and overlay 2.1.0 artifacts verify and package deterministically', async () => {
    for (const source of [vectorDirectory, overlayDirectory]) {
        const first = await loadArtifact(source);
        const second = await loadArtifact(source);
        assert.deepEqual(createArchive(first.entries), createArchive(second.entries));
        assert.equal(first.manifest.version, source === vectorDirectory ? '2.0.0' : '2.1.0');
    }
});

test('overlay contains only two replacement files and inherits the original manifest', async () => {
    const { entries } = await loadArtifact(overlayDirectory);
    const files = entries.filter(([name]) => overlay.files.some(file => file.path === name));
    assert.equal(files.length, 2);
    assert.equal(caseBindings(overlay, files).length, 57);
    assert(!entries.some(([name]) => name.includes('identity-access-token')));
    validateInheritance(overlay, await readFile(new URL('manifest.json', directory)));
});

test('overlay baseline pins the complete manifest bytes', () => {
    validateBaseline(overlay, overlayBaseline, overlayBytes);
    assert.throws(() => validateBaseline(overlay, overlayBaseline, Buffer.concat([overlayBytes, Buffer.from(' ')])), /Released manifest changed/);
});

test('overlay rejects an altered or mismatched inherited manifest', () => {
    const inherited = JSON.stringify({ ...manifest, caseCount: 1 });
    assert.throws(() => validateInheritance(overlay, inherited), /Inherited manifest changed/);
    assert.throws(() => validateInheritance(overlay, JSON.stringify({ ...manifest, version: '9.0.0' })), /Inherited version mismatch/);
});

test('overlay rejects incomplete, duplicate or unknown case ids', async () => {
    const { entries } = await loadArtifact(overlayDirectory);
    const files = entries.filter(([name]) => overlay.files.some(file => file.path === name));
    const file = overlay.files[0].path;
    const incomplete = structuredClone(overlay);
    incomplete.caseIds[file].pop();
    assert.throws(() => validateCases(incomplete, files), /Incomplete case coverage/);
    const duplicate = structuredClone(overlay);
    duplicate.caseIds[file][1] = duplicate.caseIds[file][0];
    assert.throws(() => validateCases(duplicate, files), /Duplicate case ids/);
    const unknown = structuredClone(overlay);
    unknown.caseIds['unknown.json'] = [];
    assert.throws(() => validateCases(unknown, files), /Unknown case file/);
});

test('overlay history starts at its own version without duplicating inherited baselines', () => {
    validateBaselineHistory(overlay, ['v2.1.0.json'], '# Changelog\n\n## 2.1.0\n');
    assert.throws(() => validateBaselineHistory(overlay, [], '# Changelog\n\n## 2.1.0\n'), /Missing current baseline/);
});

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
    const [major, minor] = manifest.version.split('.').map(Number);
    const version = `${major}.${minor + 1}.0`;
    const changed = { ...manifest, version };
    const updatedChangelog = `## ${version}\n\n${changelog}`;
    const updatedBaselines = [...baselineNames, `v${version}.json`];
    assert.throws(() => validateBaseline(changed, baseline), /Baseline version mismatch/);
    assert.throws(() => validateBaselineHistory(changed, baselineNames, updatedChangelog), /Missing current baseline/);
    assert.throws(() => validateBaselineHistory(changed, updatedBaselines, changelog), /Changelog version mismatch/);
    validateBaselineHistory(changed, updatedBaselines, updatedChangelog);
});

test('every older baseline must be retained and listed in the changelog', () => {
    const changed = { ...manifest, version: '1.2.0' };
    const history = '## 1.2.0\n## 1.1.0\n## 1.0.0';
    assert.throws(() => validateBaselineHistory(changed, ['v1.2.0.json', 'v1.0.0.json'], history), /retain its baseline/);
    assert.throws(() => validateBaselineHistory(changed, ['v1.2.0.json', 'v1.1.0.json', 'v1.0.0.json'], '## 1.2.0\n## 1.0.0'), /retain its baseline/);
    assert.throws(() => validateBaselineHistory(changed, ['v1.2.0.json'], '## 1.2.0'), /Missing initial baseline/);
});
