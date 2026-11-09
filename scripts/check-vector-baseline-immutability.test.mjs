// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { checkBaselineImmutability } from './check-vector-baseline-immutability.mjs';

const directory = 'Source/Identity.Specs/Vectors/arc-mcp-authorization-v1';
const tag = 'vectors/arc-mcp-authorization/v1.0.0';
const changelog = `${directory}/CHANGELOG.md`;
const contract = 'Documentation/specs/arc-mcp-authorization-v1.md';
const initialChangelog = '# Changelog\n\n## 1.0.0\n\n- Original cases.\n\n';
const initialContract = '# Contract\n\n## Vector artifact\n\nVector artifact version: 1.0.0\n\nOriginal artifact prose.\n\n### Packaging\n\nOriginal packaging prose.\n\n## Other contract terms\n\nUnrelated prose.\n\n';

async function repository(t, tagged = true) {
    const root = await mkdtemp(join(tmpdir(), 'vector-immutability-'));
    t.after(() => rm(root, { recursive: true, force: true }));
    const git = (...args) => execFileSync('git', args, { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
    const write = (path, value) => writeFile(join(root, directory, path), JSON.stringify(value));
    const writeDocument = (path, text) => writeFile(join(root, path), text);
    const manifest = { spec: 'arc-mcp-authorization', version: '1.0.0', files: [{ path: 'vectors.json' }], cases: [{ id: 'original-case' }] };
    await mkdir(join(root, directory, 'baselines'), { recursive: true });
    await mkdir(join(root, 'Documentation/specs'), { recursive: true });
    await writeDocument(changelog, initialChangelog);
    await writeDocument(contract, initialContract);
    await write('manifest.json', manifest);
    await write('vectors.json', { original: true });
    await write('baselines/v1.0.0.json', { version: '1.0.0', cases: { 'original-case': 'hash' } });
    git('init', '--initial-branch=main');
    git('-c', 'user.name=Vector Test', '-c', 'user.email=vector@example.invalid', 'add', '.');
    git('-c', 'user.name=Vector Test', '-c', 'user.email=vector@example.invalid', '-c', 'commit.gpgsign=false', 'commit', '-m', 'Initial vectors');
    if (tagged) git('tag', tag);
    const messages = [];
    t.mock.method(console, 'log', message => messages.push(message));
    return { root, git, write, writeDocument, manifest, messages };
}

async function replaceId(repo) {
    repo.manifest.cases[0].id = 'replacement-case';
    await repo.write('manifest.json', repo.manifest);
    await repo.write('baselines/v1.0.0.json', { version: '1.0.0', cases: { 'replacement-case': 'hash' } });
}

async function bump(repo) {
    repo.manifest.version = '1.1.0';
    repo.manifest.cases.push({ id: 'new-case' });
    await repo.write('manifest.json', repo.manifest);
    await repo.write('vectors.json', { original: true, added: true });
    await repo.write('baselines/v1.1.0.json', { version: '1.1.0', cases: { 'original-case': 'hash', 'new-case': 'new-hash' } });
    await repo.writeDocument(changelog, initialChangelog.replace('## 1.0.0', '## 1.1.0\n\n- Add cases.\n\n## 1.0.0'));
    await repo.writeDocument(contract, `${initialContract}## Vector artifact\n\nVector artifact version: 1.1.0\n\nAdditional cases.\n\n`);
}

test('tagged release passes unchanged', async t => {
    const repo = await repository(t);
    await checkBaselineImmutability(repo.root);
    assert(repo.messages.some(message => message.includes('1 vector baselines and 1 release tags')));
});

test('tag rejects coordinated baseline and manifest ID replacement at the same version', async t => {
    const repo = await repository(t);
    await replaceId(repo);
    await assert.rejects(checkBaselineImmutability(repo.root), /v1\.0\.0\.json changed relative to vectors\/.*new version \+ new baseline/);
});

test('tag rejects manifest-only edits at the released version', async t => {
    const repo = await repository(t);
    repo.manifest.cases[0].id = 'replacement-case';
    await repo.write('manifest.json', repo.manifest);
    await assert.rejects(checkBaselineImmutability(repo.root), /manifest\.json changed relative to vectors\//);
});

test('tag rejects vector edits even when manifest is untouched', async t => {
    const repo = await repository(t);
    await repo.write('vectors.json', { changed: true });
    await assert.rejects(checkBaselineImmutability(repo.root), /vectors\.json changed relative to vectors\//);
});

for (const tagged of [true, false]) {
    for (const path of [changelog, contract]) {
        for (const bumped of [false, true]) {
            test(`${tagged ? 'tag' : 'PR base'} rejects ${path} prose edits${bumped ? ' even after a bump' : ''}`, async t => {
                const repo = await repository(t, tagged);
                if (bumped) await bump(repo);
                const text = await readFile(join(repo.root, path), 'utf8');
                await repo.writeDocument(path, text.replace('Original', 'Revised'));
                await assert.rejects(checkBaselineImmutability(repo.root, tagged ? undefined : 'main'), /section 1\.0\.0 changed relative to/);
            });
        }
    }
}

for (const path of [changelog, contract]) {
    test(`PR base requires a new ${path} section when bumping`, async t => {
        const repo = await repository(t, false);
        const original = await readFile(join(repo.root, path), 'utf8');
        await bump(repo);
        await repo.writeDocument(path, original);
        await assert.rejects(checkBaselineImmutability(repo.root, 'main'), /requires a new section for 1\.1\.0/);
    });
}

test('version bump with new documentation sections retains released sections and passes tag and base checks', async t => {
    const repo = await repository(t);
    await bump(repo);
    await checkBaselineImmutability(repo.root, 'main');
    assert(repo.messages.some(message => message.includes('v1.1.0.json is unreleased')));
});

test('version bump cannot rewrite the old baseline', async t => {
    const repo = await repository(t);
    await bump(repo);
    await repo.write('baselines/v1.0.0.json', { regenerated: true });
    await assert.rejects(checkBaselineImmutability(repo.root), /v1\.0\.0\.json changed/);
});

test('deleted released baseline is detected from the tag even after a version bump', async t => {
    const repo = await repository(t);
    await bump(repo);
    await rm(join(repo.root, directory, 'baselines/v1.0.0.json'));
    await assert.rejects(checkBaselineImmutability(repo.root), /v1\.0\.0\.json is missing/);
});

test('untagged baseline passes with an unreleased notice', async t => {
    const repo = await repository(t, false);
    await checkBaselineImmutability(repo.root);
    assert(repo.messages.some(message => message.includes(`is unreleased (no ${tag} tag)`)));
});

test('PR base rejects coordinated baseline and manifest replacement before the first tag', async t => {
    const repo = await repository(t, false);
    await replaceId(repo);
    await assert.rejects(checkBaselineImmutability(repo.root, 'main'), /v1\.0\.0\.json changed relative to main/);
});

test('PR base rejects deleting an old untagged baseline despite a version bump', async t => {
    const repo = await repository(t, false);
    await bump(repo);
    await rm(join(repo.root, directory, 'baselines/v1.0.0.json'));
    await assert.rejects(checkBaselineImmutability(repo.root, 'main'), /v1\.0\.0\.json is missing relative to main/);
});

test('PR base rejects same-version manifest edits without regenerating the baseline', async t => {
    const repo = await repository(t, false);
    repo.manifest.cases[0].id = 'replacement-case';
    await repo.write('manifest.json', repo.manifest);
    await assert.rejects(checkBaselineImmutability(repo.root, 'main'), /manifest\.json changed relative to main/);
});

test('PR base version bump requires a new rather than reused historical baseline', async t => {
    const repo = await repository(t, false);
    await repo.write('baselines/v0.9.0.json', { version: '0.9.0' });
    repo.git('add', '.');
    repo.git('-c', 'user.name=Vector Test', '-c', 'user.email=vector@example.invalid', '-c', 'commit.gpgsign=false', 'commit', '-m', 'Retain history');
    repo.manifest.version = '0.9.0';
    await repo.write('manifest.json', repo.manifest);
    await assert.rejects(checkBaselineImmutability(repo.root, 'main'), /Version bump must add a new baseline/);
});

test('unreadable base ref fails closed', async t => {
    const repo = await repository(t, false);
    await assert.rejects(checkBaselineImmutability(repo.root, 'missing-ref'), /Command failed/);
});

test('empty baseline set cannot pass vacuously', async t => {
    const repo = await repository(t, false);
    await rm(join(repo.root, directory, 'baselines/v1.0.0.json'));
    await assert.rejects(checkBaselineImmutability(repo.root), /No vector baselines found/);
});

test('old tagged content remains available after a version bump', async t => {
    const repo = await repository(t);
    const original = await readFile(join(repo.root, directory, 'baselines/v1.0.0.json'));
    await bump(repo);
    await checkBaselineImmutability(repo.root);
    assert.deepEqual(await readFile(join(repo.root, directory, 'baselines/v1.0.0.json')), original);
});
