/**
 * A simple utility function to use in a switch statement to ensure that all possible cases have been handled.
 * If an unexpected value is encountered, it throws an error with a message and the invalid value.
 *
 * @param x - The value to check.
 * @param message - A custom exception message.
 */
export function assertUnreachable(x: never, message = 'Didn\'t expect to get here.'): never {
  throw new Error(message, { cause: { unreachable: true, invalidValue: x } });
}
