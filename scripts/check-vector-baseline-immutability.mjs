// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { readFile, readdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { parseArgs } from 'node:util';
import { loadArtifact } from './package-authorization-vectors.mjs';

const directory = 'Source/Identity.Specs/Vectors/arc-mcp-authorization-v1';
const spec = 'arc-mcp-authorization';
const changelog = `${directory}/CHANGELOG.md`;
const contract = 'Documentation/specs/arc-mcp-authorization-v1.md';
const semver = '(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)';
const baselineName = new RegExp(`^v${semver}\\.json$`);
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const remedy = 'Change requires a new version + new baseline; retain old baselines unchanged.';

function git(root, ...args) {
    return execFileSync('git', args, { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
}

function revision(root, ref) {
    return git(root, 'rev-parse', '--verify', '--end-of-options', `${ref}^{commit}`).toString().trim();
}

function filesAt(root, commit, path) {
    return git(root, 'ls-tree', '-r', '--name-only', '-z', commit, '--', path).toString().split('\0').filter(Boolean);
}

async function unchanged(root, commit, path, anchor) {
    const expected = git(root, 'show', `${commit}:${path}`);
    let actual;
    try {
        actual = await readFile(join(root, path));
    } catch (error) {
        if (error.code !== 'ENOENT') throw error;
        assert.fail(`${path} is missing relative to ${anchor}. ${remedy}`);
    }
    assert.equal(hash(actual), hash(expected), `${path} changed relative to ${anchor}. ${remedy}`);
}

function sectionVersion(heading, section, path) {
    if (path === changelog) return heading;
    if (heading !== 'Vector artifact' && heading !== 'Vector artifact 2.1.0') return undefined;
    const versions = [...section.matchAll(/^Vector artifact version: ([^\r\n]+)\r?$/gm)];
    assert.equal(versions.length, 1, `Expected one vector artifact version in ${path}`);
    assert.match(versions[0][1], new RegExp(`^${semver}$`), `Invalid vector artifact version in ${path}`);
    return versions[0][1];
}

function versionSections(text, path) {
    const headings = [...text.matchAll(/^## ([^\r\n]+)(?:\r?\n|$)/gm)];
    const sections = new Map();
    for (const [index, heading] of headings.entries()) {
        const section = text.slice(heading.index, headings[index + 1]?.index);
        const version = sectionVersion(heading[1], section, path);
        if (!version || !new RegExp(`^${semver}$`).test(version)) continue;
        assert(!sections.has(version), `Duplicate version ${version} in ${path}`);
        sections.set(version, section);
    }
    assert(sections.size > 0, `No versioned sections in ${path}`);
    return sections;
}

async function unchangedDocumentation(root, commit, manifest, anchor, versions) {
    for (const path of [changelog, contract]) {
        const expected = versionSections(git(root, 'show', `${commit}:${path}`).toString(), path);
        const actual = versionSections(await readFile(join(root, path), 'utf8'), path);
        for (const version of versions ?? expected.keys()) {
            assert(expected.has(version), `${path} has no section for ${version} at ${anchor}`);
            assert(actual.has(version), `${path} is missing section ${version} relative to ${anchor}. ${remedy}`);
            assert.equal(hash(actual.get(version)), hash(expected.get(version)), `${path} section ${version} changed relative to ${anchor}. ${remedy}`);
        }
        assert(actual.has(manifest.version), `${path} requires a new section for ${manifest.version}. ${remedy}`);
    }
}

async function unchangedVersion(root, commit, manifest, anchor) {
    const path = `${directory}/manifest.json`;
    const released = JSON.parse(git(root, 'show', `${commit}:${path}`));
    assert.equal(released.spec, spec, `Unexpected spec at ${anchor}`);
    if (manifest.version !== released.version) return;
    await unchanged(root, commit, path, anchor);
    for (const file of released.files) {
        await unchanged(root, commit, `${directory}/${file.path}`, anchor);
    }
}

async function checkTag(root, tag, manifest) {
    const version = tag.slice(`vectors/${spec}/v`.length);
    assert.match(version, new RegExp(`^${semver}$`), `Invalid release tag: ${tag}`);
    const commit = revision(root, `refs/tags/${tag}`);
    const taggedManifest = JSON.parse(git(root, 'show', `${commit}:${directory}/manifest.json`));
    assert.equal(taggedManifest.version, version, `Tag version mismatch: ${tag}`);
    await unchanged(root, commit, `${directory}/baselines/v${version}.json`, tag);
    await unchangedVersion(root, commit, manifest, tag);
    await unchangedDocumentation(root, commit, manifest, tag, [version]);
}

async function checkBase(root, baseRef, manifest) {
    const commit = revision(root, baseRef);
    const baselines = filesAt(root, commit, `${directory}/baselines`);
    for (const path of baselines) await unchanged(root, commit, path, baseRef);
    if (baselines.length === 0) {
        console.log(`Base ${baseRef} has no baselines yet (unreleased bootstrap).`);
        return;
    }
    await unchangedVersion(root, commit, manifest, baseRef);
    const previous = JSON.parse(git(root, 'show', `${commit}:${directory}/manifest.json`));
    if (manifest.version !== previous.version) {
        assert(!baselines.includes(`${directory}/baselines/v${manifest.version}.json`), `Version bump must add a new baseline. ${remedy}`);
    }
    await unchangedDocumentation(root, commit, manifest, baseRef);
    console.log(`Checked ${baselines.length} base baselines against ${baseRef}.`);
}

export async function checkBaselineImmutability(root, baseRef) {
    const manifest = JSON.parse(await readFile(join(root, directory, 'manifest.json')));
    assert.equal(manifest.spec, spec, 'Unexpected vector spec');
    assert.match(manifest.version, new RegExp(`^${semver}$`), 'Expected SemVer');
    const names = await readdir(join(root, directory, 'baselines'));
    assert(names.length > 0, 'No vector baselines found');
    assert(names.every(name => baselineName.test(name)), 'Invalid baseline filename');
    assert(names.includes(`v${manifest.version}.json`), 'Missing current baseline');
    const tags = git(root, 'for-each-ref', '--format=%(refname:strip=2)', `refs/tags/vectors/${spec}/v*`).toString().trim().split('\n').filter(Boolean);
    for (const tag of tags.filter(tag => tag !== `vectors/${spec}/v2.1.0`)) await checkTag(root, tag, manifest);
    for (const name of names) {
        const tag = `vectors/${spec}/${name.slice(0, -5)}`;
        if (!tags.includes(tag)) console.log(`Baseline ${name} is unreleased (no ${tag} tag).`);
    }
    if (baseRef) await checkBase(root, baseRef, manifest);
    await checkOverlay(root, baseRef, tags);
    console.log(`Checked ${names.length} vector baselines and ${tags.length} release tags.`);
}

async function frozenTree(root, commit, path, anchor) {
    const changed = git(root, 'diff', '--name-only', commit, '--', path).toString().trim();
    assert.equal(changed, '', `${path} changed relative to ${anchor}. ${remedy}`);
}

async function checkOverlay(root, baseRef, tags) {
    const overlay = 'Source/Identity.Specs/Vectors/v2.1.0';
    const present = (await readdir(join(root, 'Source/Identity.Specs/Vectors'))).includes('v2.1.0');
    const tag = `vectors/${spec}/v2.1.0`;
    assert(present || !tags.includes(tag), 'Released overlay is missing');
    if (!present) return;
    const { manifest } = await loadArtifact(join(root, overlay), join(root, directory));
    assert.equal(manifest.version, '2.1.0', 'Overlay version mismatch');
    const sections = versionSections(await readFile(join(root, contract), 'utf8'), contract);
    assert(sections.has(manifest.version), 'Missing overlay contract section');
    const inheritedTag = `vectors/${spec}/v${manifest.inherits.version}`;
    const anchors = [baseRef ?? 'HEAD', ...tags.filter(value => value === tag || value === inheritedTag)];
    for (const anchor of anchors) {
        const commit = revision(root, anchor.startsWith('vectors/') ? `refs/tags/${anchor}` : anchor);
        for (const name of ['arc-mcp-authorization-v1', 'arc-scope-catalog-v1', 'identity-access-token-v1', 'v2.1.0']) {
            const path = `Source/Identity.Specs/Vectors/${name}`;
            if (filesAt(root, commit, path).length > 0) await frozenTree(root, commit, path, anchor);
        }
    }
    console.log(`Checked immutable overlay ${manifest.version} and inherited 2.0.0 fixtures.`);
}

async function main() {
    const { values } = parseArgs({ options: { 'base-ref': { type: 'string' }, 'self-test': { type: 'boolean' } } });
    if (values['self-test']) {
        execFileSync(process.execPath, ['--test', fileURLToPath(new URL('./check-vector-baseline-immutability.test.mjs', import.meta.url))], { stdio: 'inherit' });
        return;
    }
    const baseRef = values['base-ref'] || process.env.BASE_REF || (process.env.GITHUB_EVENT_NAME === 'pull_request' ? 'origin/main' : undefined);
    await checkBaselineImmutability(fileURLToPath(new URL('../', import.meta.url)), baseRef);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    main().catch(error => {
        console.error(error.message);
        process.exitCode = error.code === 'ERR_ASSERTION' ? 1 : 2;
    });
}
