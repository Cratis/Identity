// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdir, readFile, readdir, writeFile } from 'node:fs/promises';
import { basename, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { gzipSync } from 'node:zlib';

const root = fileURLToPath(new URL('../', import.meta.url));
const vectorDirectory = join(root, 'Source/Identity.Specs/Vectors/arc-mcp-authorization-v1');
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

async function vectorEntries(manifest) {
    const listed = manifest.files.map(file => file.path).sort();
    const actual = (await readdir(vectorDirectory)).filter(name => !['manifest.json', 'CHANGELOG.md'].includes(name)).sort();
    assert.deepEqual(listed, actual, 'Manifest must list every vector file exactly once');
    const entries = [];
    for (const file of manifest.files) {
        assert(file.path === basename(file.path) && file.path.endsWith('.json'), 'Expected a flat JSON vector path');
        const bytes = await readFile(join(vectorDirectory, file.path));
        assert.equal(hash(bytes), file.sha256, `Vector hash mismatch: ${file.path}`);
        entries.push([file.path, bytes]);
    }
    return entries;
}

function validateCases(manifest, entries) {
    const ids = manifest.cases.map(item => item.id);
    assert(ids.every(id => /^[a-z0-9][a-z0-9-]*$/.test(id)), 'Invalid case id');
    assert.equal(new Set(ids).size, ids.length, 'Duplicate case ids');
    assert.equal(manifest.caseCount, ids.length, 'Incorrect case count');
    assert(manifest.caseCount > 0, 'Empty vector artifact');
    assert(manifest.cases.every(item => entries.some(([name]) => name === item.file)), 'Unknown case file');
    for (const [name, bytes] of entries) {
        const vectors = JSON.parse(bytes).given.vectors;
        const cases = manifest.cases.filter(item => item.file === name).sort((left, right) => left.index - right.index);
        assert.deepEqual(cases.map(item => item.index), vectors.map((_, index) => index), 'Incomplete case coverage');
        for (const item of cases) {
            assert.equal(item.sha256, hash(JSON.stringify(vectors[item.index])), `Case hash mismatch: ${item.id}`);
        }
    }
}

async function packageVectors(output) {
    const manifestBytes = await readFile(join(vectorDirectory, 'manifest.json'));
    const manifest = JSON.parse(manifestBytes);
    assert.equal(manifest.schema, 1, 'Unsupported manifest schema');
    assert.equal(manifest.spec, 'arc-mcp-authorization', 'Unexpected spec');
    assert.equal(manifest.contractVersion, 1, 'Unexpected contract version');
    assert.match(manifest.version, /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/, 'Expected SemVer');
    const entries = await vectorEntries(manifest);
    validateCases(manifest, entries);
    const changelog = await readFile(join(vectorDirectory, 'CHANGELOG.md'));
    assert.equal(changelog.toString().split('\n').find(line => line.startsWith('## ')), `## ${manifest.version}`, 'Changelog version mismatch');
    entries.push(['manifest.json', manifestBytes], ['CHANGELOG.md', changelog]);
    const archive = createArchive(entries);
    const name = `${manifest.spec}-v${manifest.contractVersion}-vectors-${manifest.version}.tar.gz`;
    await mkdir(output, { recursive: true });
    await writeFile(join(output, name), archive);
    await writeFile(join(output, `${name}.sha256`), `${hash(archive)}  ${name}\n`);
    console.log(`Packaged ${manifest.caseCount} cases: ${join(output, name)}`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    assert.equal(process.argv.length, 3, 'Usage: node scripts/package-authorization-vectors.mjs <output-directory>');
    await packageVectors(resolve(process.argv[2]));
}
