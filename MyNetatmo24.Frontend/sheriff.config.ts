import type { SheriffConfig } from '@softarc/sheriff-core';

import { noDependencies, sameTag } from '@softarc/sheriff-core';

// eslint-disable-next-line @typescript-eslint/naming-convention -- format expected by sheriff
export const config: SheriffConfig = {
  enableBarrelLess: true,
  entryFile: './src/main.ts',
  modules: {
    'src/app': {
      'domains/<domain>': {
        'feature-<feature>': ['domain:<domain>', 'type:feature'],
        'ui-<ui>': ['domain:<domain>', 'type:ui'],
        data: ['domain:<domain>', 'type:data'],
        'util-shared': ['domain:<domain>', 'type:shared'],
        'util-<util>': ['domain:<domain>', 'type:util'],
      },
    },
  },
  depRules: {
    root: ['*'],
    'domain:*': [sameTag, 'domain:shared'],
    'type:feature': ['type:data', 'type:ui', 'type:util'],
    'type:ui': ['type:data', 'type:util'],
    'type:data': ['type:util'],
    'type:util': ['type:shared'],
    'type:shared': noDependencies,
  },
};
