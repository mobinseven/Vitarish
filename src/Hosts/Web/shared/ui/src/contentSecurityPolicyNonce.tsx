/** Reads the request nonce from a CSP header produced by the ingress. */
export function contentSecurityPolicyNonce(headers: Pick<Headers, "get">): string | undefined {
  const policy = headers.get("content-security-policy");
  if (!policy) return undefined;

  const nonceDirective = policy.match(/(?:^|;)\s*(?:script-src|style-src)\s+[^;]*?'nonce-([^']+)'/i);
  return nonceDirective?.[1];
}

/** The `property` value react-aria's getNonce reads from the document head. */
export const contentSecurityPolicyNonceMetaProperty = "csp-nonce";

export interface ContentSecurityPolicyNonceMetaProps {
  nonce?: string;
}

/** Publishes the request nonce so runtime style injectors such as react-aria usePress can sign their elements. */
export function ContentSecurityPolicyNonceMeta({ nonce }: ContentSecurityPolicyNonceMetaProps) {
  if (!nonce) return null;
  return <meta property={contentSecurityPolicyNonceMetaProperty} nonce={nonce} />;
}
