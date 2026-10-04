import { cleanup, render } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import {
  ContentSecurityPolicyNonceMeta,
  contentSecurityPolicyNonce,
  contentSecurityPolicyNonceMetaProperty,
} from "../contentSecurityPolicyNonce";

afterEach(() => cleanup());

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

describe("ContentSecurityPolicyNonceMeta", () => {
  function readPublishedNonce() {
    const meta = document.querySelector<HTMLMetaElement>(`meta[property="${contentSecurityPolicyNonceMetaProperty}"]`);
    return meta ? meta.nonce || meta.content || undefined : undefined;
  }

  it("publishes the request nonce where react-aria reads it", () => {
    render(<ContentSecurityPolicyNonceMeta nonce="request-token" />);
    expect(readPublishedNonce()).toBe("request-token");
  });

  it("publishes nothing when the request has no nonce", () => {
    render(<ContentSecurityPolicyNonceMeta />);
    expect(readPublishedNonce()).toBeUndefined();
  });
});
