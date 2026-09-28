import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ContentSecurityPolicyNonceMeta, cspNonceMetaProperty } from "../ContentSecurityPolicyNonceMeta";

describe("ContentSecurityPolicyNonceMeta", () => {
  it("publishes the supplied request nonce for style insertion", () => {
    render(<ContentSecurityPolicyNonceMeta nonce="request-token" />);
    expect(document.head.querySelector(`meta[property="${cspNonceMetaProperty}"]`)).toHaveAttribute("content", "request-token");
  });

  it("does not publish an empty nonce", () => {
    render(<ContentSecurityPolicyNonceMeta />);
    expect(document.head.querySelector(`meta[property="${cspNonceMetaProperty}"][content=""]`)).toBeNull();
  });
});
