// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import js from '@eslint/js';
import ts from 'typescript-eslint';

export default ts.config(
    { ignores: ['dist/**', 'node_modules/**', '.yarn/**'] },
    js.configs.recommended,
    ...ts.configs.recommended
);
