import type { SheriffConfig } from '@softarc/sheriff-core';

import { noDependencies, sameTag } from '@softarc/sheriff-core';

export const config: SheriffConfig = {
  enableBarrelLess: true,
  entryFile: './src/main.ts',
  autoTagging: false,
  modules: {
    'src/app': {
      'domain/<domain>': {
        'feature-<feature>': ['domain:<domain>', 'type:feature'],
        'ui-<ui>': ['domain:<domain>', 'type:ui'],
        data: ['domain:<domain>', 'type:data'],
        'util-<util>': ['domain:<domain>', 'type:util'],
      }
    }
  },
  depRules: {
    'root': ['*'],
    'domain:*': [sameTag, 'domain:shared'],
    'type:feature': ['type:data', 'type:ui', 'type:util'],
    'type:ui': ['type:data', 'type:util'],
    'type:data': ['type:util'],
    'type:util': noDependencies,
  },
};
