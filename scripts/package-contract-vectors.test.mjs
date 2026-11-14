// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { access, mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { gunzipSync } from 'node:zlib';
import { checkBundle, checkReleased, collectVectorEntries, discoverFolders, overlayFolder, packageBundle, tagPrefix, vectorsDirectory, writeBundle } from './package-contract-vectors.mjs';

const repository = fileURLToPath(new URL('../', import.meta.url));
const coverageFor = names => Object.fromEntries(names.map(name => [name, { identityExecutable: false, readers: [], notApplicable: [] }]));
const changelogFor = version => `# Changelog\n\n## ${version}\n\n- Test bundle.\n`;

async function put(directory, path, content) {
    await mkdir(join(directory, path, '..'), { recursive: true });
    await writeFile(join(directory, path), content);
}

async function fixture(version = '1.0.0') {
    const directory = await mkdtemp(join(tmpdir(), 'contract-vectors-'));
    await put(directory, 'alpha-thing-v1/a.json', '{"a":1}');
    await put(directory, 'beta-thing-v2/nested/b.json', '{"b":2}');
    await put(directory, 'v2.1.0/replacement.json', '{"r":1}');
    const names = ['alpha-thing-v1', 'beta-thing-v2', overlayFolder];
    await put(directory, 'bundle/coverage.json', JSON.stringify(coverageFor(names)));
    await put(directory, 'bundle/CHANGELOG.md', changelogFor(version));
    await writeBundle(directory, version);
    return directory;
}

const rejects = (promise, pattern) => assert.rejects(promise, pattern);

test('the real tree is bundled completely, including every discovered folder and the overlay', async () => {
    const folders = await discoverFolders(vectorsDirectory);
    assert.equal(folders.length, 15);
    assert(folders.includes('identity-credential-record-v1'));
    const { manifest, entries } = await checkBundle(vectorsDirectory);
    assert.deepEqual(manifest.folders.map(folder => folder.name).sort(), [...folders, overlayFolder].sort());
    assert.equal(manifest.fileCount, entries.length);
    for (const folder of manifest.folders) assert(folder.fileCount > 0, `Empty folder ${folder.name}`);
});

test('a folder added to the tree is picked up and a stale manifest is rejected', async () => {
    const directory = await fixture();
    try {
        await put(directory, 'gamma-thing-v1/c.json', '{"c":3}');
        assert((await collectVectorEntries(directory)).some(([path]) => path === 'gamma-thing-v1/c.json'));
        await rejects(checkBundle(directory), /stale for: gamma-thing-v1\/c\.json/);
    } finally { await rm(directory, { recursive: true }); }
});

test('an unrecognized top-level directory fails discovery instead of being skipped', async () => {
    const directory = await fixture();
    try {
        await put(directory, 'scratch/x.json', '{}');
        await rejects(discoverFolders(directory), /Unrecognized directory/);
    } finally { await rm(directory, { recursive: true }); }
});

test('a vector edit without regenerating the manifest fails, and regenerating without a baseline fails', async () => {
    const directory = await fixture();
    try {
        await checkBundle(directory);
        await put(directory, 'alpha-thing-v1/a.json', '{"a":2}');
        await rejects(checkBundle(directory), /stale for: alpha-thing-v1\/a\.json/);
        await writeBundle(directory, '1.0.0');
        await checkBundle(directory);
        await writeBundle(directory, '1.1.0');
        await rejects(checkBundle(directory), /Changelog version mismatch/);
        await put(directory, 'bundle/CHANGELOG.md', `${changelogFor('1.1.0')}\n## 1.0.0\n\n- First.\n`);
        await checkBundle(directory);
        await rm(join(directory, 'bundle/baselines/v1.0.0.json'));
        await rejects(checkBundle(directory), /Missing initial baseline/);
    } finally { await rm(directory, { recursive: true }); }
});

test('a baseline that does not pin the current manifest is rejected', async () => {
    const directory = await fixture();
    try {
        await put(directory, 'bundle/baselines/v1.0.0.json', JSON.stringify({ version: '1.0.0', manifestSha256: '0'.repeat(64) }));
        await rejects(checkBundle(directory), /does not pin the current manifest/);
    } finally { await rm(directory, { recursive: true }); }
});

test('coverage must describe every bundled folder', async () => {
    const directory = await fixture();
    try {
        await put(directory, 'bundle/coverage.json', JSON.stringify(coverageFor(['alpha-thing-v1'])));
        await writeBundle(directory, '1.0.0');
        await rejects(checkBundle(directory), /Coverage must describe every bundled folder/);
    } finally { await rm(directory, { recursive: true }); }
});

test('the real coverage names existing reader specs and marks reader-less folders not executable', async () => {
    const { manifest } = await checkBundle(vectorsDirectory);
    for (const [name, entry] of Object.entries(manifest.coverage)) {
        for (const reader of entry.readers) await access(join(repository, reader));
        assert.equal(entry.identityExecutable, entry.readers.length > 0, name);
    }
});

test('the archive is deterministic and keeps every path within the ustar name limit', async () => {
    const output = await mkdtemp(join(tmpdir(), 'contract-vectors-out-'));
    try {
        const first = await packageBundle(output, vectorsDirectory);
        const bytes = await readFile(join(output, first));
        await packageBundle(output, vectorsDirectory);
        assert.deepEqual(await readFile(join(output, first)), bytes);
        const tar = gunzipSync(bytes);
        const names = [];
        for (let offset = 0; offset + 512 <= tar.length && tar[offset] !== 0; offset += 512 + Math.ceil(parseInt(tar.subarray(offset + 124, offset + 135).toString(), 8) / 512) * 512) {
            names.push(tar.subarray(offset, offset + 100).toString().replace(/\0.*$/, ''));
        }
        assert(names.length > 200);
        assert(names.every(name => Buffer.byteLength(name) <= 100));
        assert(names.includes('manifest.json') && names.includes('CHANGELOG.md') && names.includes('baselines/v1.0.0.json'));
        assert(names.some(name => name.startsWith(`${overlayFolder}/`)));
        assert((await readdir(output)).sort().join() === `${first},${first}.sha256`);
    } finally { await rm(output, { recursive: true }); }
});

test('a tag build packages only a matching bundle version', async () => {
    const directory = await fixture();
    const output = await mkdtemp(join(tmpdir(), 'contract-vectors-out-'));
    try {
        await packageBundle(output, directory, `${tagPrefix}1.0.0`);
        await rejects(packageBundle(output, directory, `${tagPrefix}2.0.0`), /does not match the bundle version/);
        await rejects(packageBundle(output, directory, 'vectors/arc-mcp-authorization/v2.0.0'), /does not match the bundle version/);
    } finally { await rm(directory, { recursive: true }); await rm(output, { recursive: true }); }
});

test('a released tag protects the manifest, baseline and changelog; the first release has no tag and passes', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'contract-vectors-git-'));
    try {
        const git = (...args) => execFileSync('git', ['-c', 'user.name=t', '-c', 'user.email=t@example.test', '-c', 'commit.gpgsign=false', '-c', 'tag.gpgsign=false', ...args], { cwd: directory, stdio: 'pipe' });
        git('init', '-q');
        const vectors = join(directory, 'Vectors');
        await put(vectors, 'alpha-thing-v1/a.json', '{"a":1}');
        await put(vectors, 'v2.1.0/replacement.json', '{"r":1}');
        await put(vectors, 'bundle/coverage.json', JSON.stringify(coverageFor(['alpha-thing-v1', overlayFolder])));
        await put(vectors, 'bundle/CHANGELOG.md', changelogFor('1.0.0'));
        await writeBundle(vectors, '1.0.0');
        await checkReleased(directory, 'Vectors');
        git('add', '-A');
        git('commit', '-q', '-m', 'release');
        git('tag', `${tagPrefix}1.0.0`);
        await checkReleased(directory, 'Vectors');
        await put(vectors, 'alpha-thing-v1/a.json', '{"a":2}');
        await writeBundle(vectors, '1.0.0', [`${tagPrefix}1.0.0`]).then(() => assert.fail('Expected a released version to be refused'), error => assert.match(error.message, /already released/));
        await put(vectors, 'bundle/manifest.json', `${await readFile(join(vectors, 'bundle/manifest.json'), 'utf8')} `);
        await rejects(checkReleased(directory, 'Vectors'), /without a version bump/);
    } finally { await rm(directory, { recursive: true }); }
});
