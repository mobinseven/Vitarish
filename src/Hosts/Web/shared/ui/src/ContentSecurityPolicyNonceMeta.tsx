import { cspNonceMetaProperty } from "./contentSecurityPolicyNonceMetaProperty.cjs";

export { cspNonceMetaProperty };

export interface ContentSecurityPolicyNonceMetaProps {
  nonce?: string;
}

export function ContentSecurityPolicyNonceMeta({ nonce }: ContentSecurityPolicyNonceMetaProps) {
  if (!nonce) return null;

  return <meta property={cspNonceMetaProperty} content={nonce} />;
}
