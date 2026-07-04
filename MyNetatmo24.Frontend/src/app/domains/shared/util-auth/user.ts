import * as z from 'zod/mini';

export const USER_SCHEMA = z.strictObject({
  isAuthenticated: z.boolean(),
  name: z.nullable(z.string()),
  claims: z.array(
    z.strictObject({
      type: z.string(),
      value: z.string(),
    })
  ),
});

export type User = z.infer<typeof USER_SCHEMA>;
