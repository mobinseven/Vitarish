/** Reads a static URL from its generated client so browser specs share the source-owned route. */
export function generatedOperationPath(operation: { toString(): string }): string {
  const match = /url:\s*[`"']([^`"']+)[`"']/.exec(operation.toString());
  if (!match) throw new Error("The generated client no longer exposes a static operation URL.");
  return match[1];
}
