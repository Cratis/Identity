// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { gunzipSync } from 'node:zlib';
import { createArchive } from './package-authorization-vectors.mjs';

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
