/** Reads the request nonce from a CSP header produced by the ingress. */
export function contentSecurityPolicyNonce(headers: Pick<Headers, "get">): string | undefined {
  const policy = headers.get("content-security-policy");
  if (!policy) return undefined;

  const nonceDirective = policy.match(/(?:^|;)\s*(?:script-src|style-src)\s+[^;]*?'nonce-([^']+)'/i);
  return nonceDirective?.[1];
}
