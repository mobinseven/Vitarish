import { describe, expect, it } from "vitest";
import { contentSecurityPolicyNonce } from "../contentSecurityPolicyNonce";

describe("contentSecurityPolicyNonce", () => {
  it("reads the request nonce from the ingress policy", () => {
    const headers = new Headers({ "content-security-policy": "default-src 'self'; script-src 'self' 'nonce-request-token' 'unsafe-inline'; style-src 'self' 'nonce-request-token' 'unsafe-inline'" });
    expect(contentSecurityPolicyNonce(headers)).toBe("request-token");
  });

  it("does not accept a nonce from unrelated directives", () => {
    const headers = new Headers({ "content-security-policy": "default-src 'nonce-unrelated'; script-src 'self'; style-src 'self'" });
    expect(contentSecurityPolicyNonce(headers)).toBeUndefined();
    expect(contentSecurityPolicyNonce(new Headers())).toBeUndefined();
  });
});
