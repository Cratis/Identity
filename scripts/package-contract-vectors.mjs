// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Packages every contract vector folder as one versioned, checksummed bundle.
// Usage: node scripts/package-contract-vectors.mjs check | write [--version <semver>] | package <output-directory>

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdir, readFile, readdir, writeFile } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { parseArgs } from 'node:util';
import { createArchive, validateBaselineHistory } from './package-authorization-vectors.mjs';

const root = fileURLToPath(new URL('../', import.meta.url));
export const vectorsDirectory = join(root, 'Source/Identity.Specs/Vectors');
export const bundleId = 'identity-contract-vectors';
export const tagPrefix = `vectors/${bundleId}/v`;
const folderName = /^[a-z]+(-[a-z]+)*-v[0-9]+$/;
const overlaySource = 'v2.1.0';
export const overlayFolder = 'arc-mcp-authorization-v1-overlay-2.1.0';
const metadataSource = 'bundle';
const semver = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/;
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const byPath = ([left], [right]) => left < right ? -1 : left > right ? 1 : 0;
const remedy = 'Run `node scripts/package-contract-vectors.mjs write` (after bumping the version for a released bundle), add a changelog entry, and commit the result.';

async function listFiles(directory, prefix = '') {
    const entries = await readdir(join(directory, prefix), { withFileTypes: true });
    const nested = await Promise.all(entries.map(entry => {
        const path = prefix ? `${prefix}/${entry.name}` : entry.name;
        return entry.isDirectory() ? listFiles(directory, path) : [path];
    }));
    return nested.flat().sort();
}

/** Discovers the vector folders by naming convention; an unrecognized top-level directory is an error, never skipped. */
export async function discoverFolders(vectors) {
    const entries = (await readdir(vectors, { withFileTypes: true })).filter(entry => entry.isDirectory());
    const unknown = entries.map(entry => entry.name).filter(name => !folderName.test(name) && name !== overlaySource && name !== metadataSource);
    assert.deepEqual(unknown, [], `Unrecognized directory under the vectors folder: ${unknown.join(', ')}`);
    const folders = entries.map(entry => entry.name).filter(name => folderName.test(name)).sort();
    assert(folders.length > 0, 'No vector folders discovered');
    return folders;
}

async function collectFolder(vectors, source, target) {
    const files = await listFiles(vectors, source);
    return Promise.all(files.map(async file => [`${target}/${file.slice(source.length + 1)}`, await readFile(join(vectors, file))]));
}

/** Every vector file as [archive path, bytes]: each discovered folder plus the 2.1.0 overlay under its own prefix. */
export async function collectVectorEntries(vectors) {
    const folders = await discoverFolders(vectors);
    const groups = folders.map(name => collectFolder(vectors, name, name));
    groups.push(collectFolder(vectors, overlaySource, overlayFolder));
    return (await Promise.all(groups)).flat().sort(byPath);
}

function folderCounts(entries) {
    const counts = new Map();
    for (const [path] of entries) {
        const name = path.slice(0, path.indexOf('/'));
        counts.set(name, (counts.get(name) ?? 0) + 1);
    }
    return [...counts].map(([name, fileCount]) => ({ name, fileCount }));
}

export function buildManifest(version, entries, coverage) {
    assert.match(version, semver, 'Expected SemVer');
    return {
        schema: 1,
        bundle: bundleId,
        version,
        fileCount: entries.length,
        folders: folderCounts(entries),
        files: entries.map(([path, bytes]) => ({ path, sha256: hash(bytes) })),
        coverage: Object.fromEntries(Object.entries(coverage).sort(([left], [right]) => left < right ? -1 : 1)),
    };
}

export const serialize = manifest => `${JSON.stringify(manifest, null, 2)}\n`;

export function validateCoverage(manifest) {
    const folders = manifest.folders.map(folder => folder.name).sort();
    assert.deepEqual(Object.keys(manifest.coverage).sort(), folders, 'Coverage must describe every bundled folder exactly once');
    for (const [name, entry] of Object.entries(manifest.coverage)) {
        assert.equal(typeof entry.identityExecutable, 'boolean', `Coverage ${name}: identityExecutable must be boolean`);
        assert(Array.isArray(entry.readers) && Array.isArray(entry.notApplicable), `Coverage ${name}: readers and notApplicable must be arrays`);
        assert.equal(entry.identityExecutable, entry.readers.length > 0, `Coverage ${name}: identityExecutable must match readers`);
        for (const item of entry.notApplicable) assert(item.implementer && item.reason, `Coverage ${name}: notApplicable needs implementer and reason`);
    }
}

function staleFiles(expected, actual) {
    const left = new Map(expected.files.map(file => [file.path, file.sha256]));
    const right = new Map(actual.files.map(file => [file.path, file.sha256]));
    const paths = new Set([...left.keys(), ...right.keys()]);
    return [...paths].filter(path => left.get(path) !== right.get(path)).sort();
}

async function readMetadata(vectors) {
    const directory = join(vectors, metadataSource);
    const manifestBytes = await readFile(join(directory, 'manifest.json'));
    const coverage = JSON.parse(await readFile(join(directory, 'coverage.json')));
    const changelog = await readFile(join(directory, 'CHANGELOG.md'), 'utf8');
    const baselines = await Promise.all((await readdir(join(directory, 'baselines'))).sort().map(async name => [name, await readFile(join(directory, 'baselines', name))]));
    return { manifestBytes, manifest: JSON.parse(manifestBytes), coverage, changelog, baselines };
}

function validateBaselines(metadata) {
    const { manifest, manifestBytes, changelog, baselines } = metadata;
    validateBaselineHistory(manifest, baselines.map(([name]) => name), changelog);
    for (const [name, bytes] of baselines) {
        assert.match(name, /^v(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\.json$/, 'Invalid baseline filename');
        const baseline = JSON.parse(bytes);
        assert.equal(`v${baseline.version}.json`, name, 'Baseline filename version mismatch');
        if (baseline.version === manifest.version) assert.equal(baseline.manifestSha256, hash(manifestBytes), 'Baseline does not pin the current manifest');
    }
}

/** Verifies the committed manifest, coverage, changelog and baselines against the vector tree; returns the vector entries. */
export async function checkBundle(vectors) {
    const metadata = await readMetadata(vectors);
    const { manifest, manifestBytes, coverage } = metadata;
    assert.equal(manifest.bundle, bundleId, 'Unexpected bundle id');
    const entries = await collectVectorEntries(vectors);
    const expected = buildManifest(manifest.version, entries, coverage);
    const stale = staleFiles(expected, manifest);
    assert.deepEqual(stale, [], `Bundle manifest is stale for: ${stale.join(', ')}. ${remedy}`);
    assert.equal(serialize(expected), manifestBytes.toString(), `Bundle manifest does not match the vector tree. ${remedy}`);
    validateCoverage(manifest);
    validateBaselines(metadata);
    return { ...metadata, entries };
}

function git(repository, ...args) {
    return execFileSync('git', args, { cwd: repository, stdio: ['ignore', 'pipe', 'pipe'] });
}

const sectionOf = (changelog, version) => {
    const start = changelog.indexOf(`## ${version}\n`);
    const next = changelog.indexOf('\n## ', start + 1);
    return start < 0 ? undefined : changelog.slice(start, next < 0 ? undefined : next);
};

/** A released bundle tag pins its manifest, baseline and changelog section; the first release has no tag yet and passes. */
export async function checkReleased(repository, vectorsRelative) {
    const tags = git(repository, 'for-each-ref', '--format=%(refname:strip=2)', `refs/tags/${tagPrefix}*`).toString().split('\n').filter(Boolean);
    const metadata = await readMetadata(join(repository, vectorsRelative));
    for (const tag of tags) {
        const version = tag.slice(tagPrefix.length);
        assert.match(version, semver, `Invalid release tag: ${tag}`);
        const at = path => git(repository, 'show', `refs/tags/${tag}:${vectorsRelative}/${metadataSource}/${path}`);
        const released = JSON.parse(at('manifest.json'));
        assert.equal(released.version, version, `Tag version mismatch: ${tag}`);
        const baseline = metadata.baselines.find(([name]) => name === `v${version}.json`);
        assert(baseline, `Released baseline v${version}.json is missing. Retain old baselines unchanged.`);
        assert.equal(hash(baseline[1]), hash(at(`baselines/v${version}.json`)), `Released baseline v${version}.json changed relative to ${tag}`);
        assert.equal(sectionOf(metadata.changelog, version), sectionOf(at('CHANGELOG.md').toString(), version), `Changelog section ${version} changed relative to ${tag}`);
        if (metadata.manifest.version === version) assert.equal(hash(metadata.manifestBytes), hash(at('manifest.json')), `Vectors changed without a version bump relative to ${tag}. ${remedy}`);
    }
    console.log(`Checked the bundle against ${tags.length} released tag(s).`);
}

/** Regenerates manifest.json and the current version's baseline; a released version cannot be rewritten. */
export async function writeBundle(vectors, version, tags = []) {
    const directory = join(vectors, metadataSource);
    const coverage = JSON.parse(await readFile(join(directory, 'coverage.json')));
    const existing = await readFile(join(directory, 'manifest.json')).then(bytes => JSON.parse(bytes).version, () => undefined);
    const target = version ?? existing ?? '1.0.0';
    assert(!tags.includes(`${tagPrefix}${target}`), `Version ${target} is already released; pass --version with a new version.`);
    const manifest = buildManifest(target, await collectVectorEntries(vectors), coverage);
    const bytes = serialize(manifest);
    await mkdir(join(directory, 'baselines'), { recursive: true });
    await writeFile(join(directory, 'manifest.json'), bytes);
    await writeFile(join(directory, 'baselines', `v${target}.json`), `${JSON.stringify({ bundle: bundleId, version: target, manifestSha256: hash(bytes) }, null, 2)}\n`);
    return manifest;
}

export async function packageBundle(output, vectors, tag) {
    const { manifest, manifestBytes, changelog, baselines, entries } = await checkBundle(vectors);
    if (tag) assert.equal(tag, `${tagPrefix}${manifest.version}`, 'The release tag does not match the bundle version');
    const archiveEntries = [...entries, ['manifest.json', manifestBytes], ['CHANGELOG.md', Buffer.from(changelog)], ...baselines.map(([name, bytes]) => [`baselines/${name}`, bytes])];
    const archive = createArchive(archiveEntries);
    const name = `${bundleId}-${manifest.version}.tar.gz`;
    await mkdir(output, { recursive: true });
    await writeFile(join(output, name), archive);
    await writeFile(join(output, `${name}.sha256`), `${hash(archive)}  ${name}\n`);
    console.log(`Packaged ${entries.length} vector files from ${manifest.folders.length} folders: ${join(output, name)}`);
    return name;
}

const releasedTags = () => git(root, 'for-each-ref', '--format=%(refname:strip=2)', `refs/tags/${tagPrefix}*`).toString().split('\n').filter(Boolean);

async function main(argv) {
    const { positionals, values } = parseArgs({ args: argv, allowPositionals: true, options: { version: { type: 'string' } } });
    const [command, output] = positionals;
    if (command === 'check') {
        const { manifest, entries } = await checkBundle(vectorsDirectory);
        console.log(`Bundle ${manifest.version}: ${entries.length} files in ${manifest.folders.length} folders match the manifest.`);
        return checkReleased(root, 'Source/Identity.Specs/Vectors');
    }
    if (command === 'write') {
        const manifest = await writeBundle(vectorsDirectory, values.version, releasedTags());
        return console.log(`Wrote bundle ${manifest.version} manifest for ${manifest.fileCount} files.`);
    }
    assert(command === 'package' && output, 'Usage: node scripts/package-contract-vectors.mjs check | write [--version <semver>] | package <output-directory>');
    const tag = process.env.GITHUB_REF_TYPE === 'tag' ? process.env.GITHUB_REF_NAME : undefined;
    await packageBundle(resolve(output), vectorsDirectory, tag);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    await main(process.argv.slice(2));
}
