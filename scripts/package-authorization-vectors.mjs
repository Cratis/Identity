// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdir, readFile, readdir, writeFile } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { gzipSync } from 'node:zlib';

const root = fileURLToPath(new URL('../', import.meta.url));
export const vectorDirectory = join(root, 'Source/Identity.Specs/Vectors/arc-mcp-authorization-v1');
export const overlayDirectory = join(root, 'Source/Identity.Specs/Vectors/v2.1.0');
const hash = bytes => createHash('sha256').update(bytes).digest('hex');

function tarHeader(name, size) {
    assert(Buffer.byteLength(name) <= 100, 'Archive path exceeds the ustar name field');
    const header = Buffer.alloc(512);
    header.write(name);
    const octal = (value, width) => `${value.toString(8).padStart(width - 1, '0')}\0`;
    header.write(octal(0o644, 8), 100);
    header.write(octal(0, 8), 108);
    header.write(octal(0, 8), 116);
    header.write(octal(size, 12), 124);
    header.write(octal(0, 12), 136);
    header.fill(32, 148, 156);
    header.write('0', 156);
    header.write('ustar\0', 257);
    header.write('00', 263);
    const checksum = header.reduce((sum, byte) => sum + byte, 0);
    header.write(`${checksum.toString(8).padStart(6, '0')}\0 `, 148);
    return header;
}

export function createArchive(entries) {
    const blocks = [];
    for (const [name, bytes] of [...entries].sort(([left], [right]) => left < right ? -1 : left > right ? 1 : 0)) {
        blocks.push(tarHeader(name, bytes.length), bytes, Buffer.alloc((512 - bytes.length % 512) % 512));
    }
    blocks.push(Buffer.alloc(1024));
    const archive = gzipSync(Buffer.concat(blocks), { level: 9 });
    archive[9] = 255; // Normalize the gzip OS byte; the default mtime is already zero.
    return archive;
}

async function vectorFiles(directory, prefix = '') {
    const entries = await readdir(join(directory, prefix), { withFileTypes: true });
    const files = await Promise.all(entries.filter(entry => prefix || !['manifest.json', 'CHANGELOG.md', 'baselines'].includes(entry.name)).map(entry => {
        const path = prefix ? `${prefix}/${entry.name}` : entry.name;
        return entry.isDirectory() ? vectorFiles(directory, path) : [path];
    }));
    return files.flat();
}

async function vectorEntries(manifest, directory) {
    const listed = manifest.files.map(file => file.path).sort();
    const actual = (await vectorFiles(directory)).sort();
    assert.deepEqual(listed, actual, 'Manifest must list every vector file exactly once');
    const entries = [];
    for (const file of manifest.files) {
        assert.match(file.path, /^[a-z0-9-]+(?:\/[a-z0-9-]+)*\.json$/, 'Expected a relative JSON vector path');
        const bytes = await readFile(join(directory, file.path));
        assert.equal(hash(bytes), file.sha256, `Vector hash mismatch: ${file.path}`);
        entries.push([file.path, bytes]);
    }
    return entries;
}

export function caseBindings(manifest, entries) {
    if (manifest.schema !== 2) return manifest.cases;
    assert.deepEqual(Object.keys(manifest.caseIds).sort(), entries.map(([name]) => name).sort(), 'Unknown case file');
    return entries.flatMap(([file, bytes]) => {
        const vectors = JSON.parse(bytes).given.vectors;
        const ids = manifest.caseIds[file];
        assert.equal(ids.length, vectors.length, 'Incomplete case coverage');
        return ids.map((id, index) => ({ id, file, index, sha256: hash(JSON.stringify(vectors[index])) }));
    });
}

export function validateCases(manifest, entries) {
    const bindings = caseBindings(manifest, entries);
    const ids = bindings.map(item => item.id);
    assert(ids.every(id => /^[a-z0-9][a-z0-9-]*$/.test(id)), 'Invalid case id');
    assert.equal(new Set(ids).size, ids.length, 'Duplicate case ids');
    assert.equal(manifest.caseCount, ids.length, 'Incorrect case count');
    assert(manifest.caseCount > 0, 'Empty vector artifact');
    assert(bindings.every(item => entries.some(([name]) => name === item.file)), 'Unknown case file');
    for (const [name, bytes] of entries) {
        const vectors = JSON.parse(bytes).given.vectors;
        const cases = bindings.filter(item => item.file === name).sort((left, right) => left.index - right.index);
        assert.deepEqual(cases.map(item => item.index), vectors.map((_, index) => index), 'Incomplete case coverage');
        for (const item of cases) {
            assert.equal(item.sha256, hash(JSON.stringify(vectors[item.index])), `Case hash mismatch: ${item.id}`);
        }
    }
}

export function validateBaseline(manifest, baseline, manifestBytes) {
    assert.equal(manifest.version, baseline.version, 'Baseline version mismatch');
    if (manifest.schema === 2) {
        assert.equal(hash(manifestBytes), baseline.manifestSha256, 'Released manifest changed without a version bump');
        return;
    }
    const cases = Object.fromEntries(manifest.cases.map(item => [item.id, item.sha256]));
    const files = Object.fromEntries(manifest.files.map(item => [item.path, item.sha256]));
    assert.deepEqual(cases, baseline.cases, 'Released case bindings changed without a version bump');
    assert.deepEqual(files, baseline.files, 'Released file hashes changed without a version bump');
}

export function validateBaselineHistory(manifest, names, changelog) {
    const headings = changelog.split('\n').filter(line => line.startsWith('## '));
    assert.equal(headings[0], `## ${manifest.version}`, 'Changelog version mismatch');
    assert(names.includes(`v${manifest.version}.json`), 'Missing current baseline');
    if (!manifest.inherits) assert(names.includes('v1.0.0.json'), 'Missing initial baseline');
    assert.deepEqual([...names].sort(), headings.map(line => `v${line.slice(3)}.json`).sort(), 'Every release must retain its baseline and changelog heading');
}

async function baselineEntries(manifest, changelog, source, manifestBytes) {
    const directory = join(source, 'baselines');
    const names = await readdir(directory);
    validateBaselineHistory(manifest, names, changelog);
    const entries = [];
    for (const name of names) {
        assert.match(name, /^v(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\.json$/, 'Invalid baseline filename');
        const bytes = await readFile(join(directory, name));
        const baseline = JSON.parse(bytes);
        assert.equal(`v${baseline.version}.json`, name, 'Baseline filename version mismatch');
        if (baseline.version === manifest.version) validateBaseline(manifest, baseline, manifestBytes);
        entries.push([`baselines/${name}`, bytes]);
    }
    return entries;
}

export function validateInheritance(manifest, inherited) {
    assert.equal(manifest.inherits.version, JSON.parse(inherited).version, 'Inherited version mismatch');
    assert.equal(manifest.inherits.sha256, hash(inherited), 'Inherited manifest changed');
}

export async function loadArtifact(directory = vectorDirectory, inheritedDirectory = vectorDirectory) {
    const manifestBytes = await readFile(join(directory, 'manifest.json'));
    const manifest = JSON.parse(manifestBytes);
    assert([1, 2].includes(manifest.schema), 'Unsupported manifest schema');
    if (manifest.schema === 2) assert(manifest.inherits, 'Overlay requires an inherited version');
    assert.equal(manifest.spec, 'arc-mcp-authorization', 'Unexpected spec');
    assert.equal(manifest.contractVersion, 1, 'Unexpected contract version');
    assert.match(manifest.version, /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/, 'Expected SemVer');
    if (manifest.inherits) {
        const inherited = await readFile(join(inheritedDirectory, 'manifest.json'));
        validateInheritance(manifest, inherited);
    }
    const entries = await vectorEntries(manifest, directory);
    validateCases(manifest, entries);
    const changelog = await readFile(join(directory, 'CHANGELOG.md'));
    const baselines = await baselineEntries(manifest, changelog.toString(), directory, manifestBytes);
    entries.push(['manifest.json', manifestBytes], ['CHANGELOG.md', changelog], ...baselines);
    return { manifest, entries };
}

export async function packageVectors(output, directory = vectorDirectory) {
    const { manifest, entries } = await loadArtifact(directory);
    const archive = createArchive(entries);
    const name = `${manifest.spec}-v${manifest.contractVersion}-vectors-${manifest.version}.tar.gz`;
    await mkdir(output, { recursive: true });
    await writeFile(join(output, name), archive);
    await writeFile(join(output, `${name}.sha256`), `${hash(archive)}  ${name}\n`);
    console.log(`Packaged ${manifest.caseCount} cases: ${join(output, name)}`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    assert.equal(process.argv.length, 3, 'Usage: node scripts/package-authorization-vectors.mjs <output-directory>');
    const tag = process.env.GITHUB_REF_TYPE === 'tag' ? process.env.GITHUB_REF_NAME : undefined;
    const artifacts = [vectorDirectory, overlayDirectory];
    let packaged = 0;
    for (const directory of artifacts) {
        const { manifest } = await loadArtifact(directory);
        if (tag && tag !== `vectors/${manifest.spec}/v${manifest.version}`) continue;
        await packageVectors(resolve(process.argv[2]), directory);
        packaged++;
    }
    assert(packaged > 0, 'No vector artifact matches the release tag');
}
