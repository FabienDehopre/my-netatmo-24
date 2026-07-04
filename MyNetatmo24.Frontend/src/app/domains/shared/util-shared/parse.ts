import * as z from 'zod/mini';

export function parse<Schema extends z.z.core.$ZodType>(schema: Schema): (raw: unknown) => z.z.core.output<Schema> {
  return (raw: unknown): z.z.core.output<Schema> => {
    try {
      return z.parse(schema, raw);
    } catch (error) {
      if (ngDevMode) {
        console.error('Validation error:', error);
      }
      throw error;
    }
  };
}

export function parseCollection<Schema extends z.z.core.$ZodType>(
  schema: Schema
): (raw: unknown) => z.z.core.output<Schema>[] {
  return (raw: unknown): z.z.core.output<Schema>[] => {
    try {
      return z.parse(z.array(schema), raw);
    } catch (error) {
      if (ngDevMode) {
        console.error('Validation error:', error);
      }
      throw error;
    }
  };
}
