import { of } from 'rxjs';
import { describe, expect, test } from 'vitest';

import { filterNullish } from './filter-nullish';

describe('filterNullish operator', () => {
  test('filters out null values', () => {
    const source = of(1, null, 2, null, 3);
    const results: number[] = [];

    source.pipe(filterNullish()).subscribe((value) => {
      results.push(value);
    });

    expect(results).toEqual([1, 2, 3]);
  });

  test('filters out undefined values', () => {
    const source = of(1, undefined, 2, undefined, 3);
    const results: number[] = [];

    source.pipe(filterNullish()).subscribe((value) => {
      results.push(value);
    });

    expect(results).toEqual([1, 2, 3]);
  });

  test('passes through non-nullish values', () => {
    const source = of(0, '', false, {}, []);
    const results: unknown[] = [];

    source.pipe(filterNullish()).subscribe((value) => {
      results.push(value);
    });

    expect(results).toEqual([0, '', false, {}, []]);
  });
});
