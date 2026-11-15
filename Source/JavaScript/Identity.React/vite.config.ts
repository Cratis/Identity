// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';
import dts from 'vite-plugin-dts';

export default defineConfig({
    plugins: [dts({ include: ['src'], exclude: ['src/**/for_*/**'] })],
    build: {
        lib: { entry: 'src/index.ts', formats: ['es'], fileName: 'index' },
        rollupOptions: { external: [/^react(?:\/|$)/, /^@cratis\//] }
    },
    test: { environment: 'jsdom', include: ['src/**/for_*/**/*.spec.tsx'] }
});
