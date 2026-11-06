/** Builds an MSW matcher from the URL in an Orval generated operation. */
export function generatedOperationRequestPattern(operation: { toString(): string }): RegExp {
  const url = /url:\s*[`"']([^`"']+)[`"']/.exec(operation.toString())?.[1];
  if (!url) throw new Error("The generated operation no longer exposes its request URL.");

  const escapedUrl = url.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  return new RegExp(`${escapedUrl}(?:\\?|$)`);
}
