import { describe, expect, it } from "vitest";
import {
  getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey,
} from "../api/generated/intelligence/intelligence";
import {
  getIntelligenceRewriteMutationOptions,
  getIntelligenceStreamRewriteMutationOptions,
  intelligenceRewrite,
  intelligenceStreamRewrite,
  useIntelligenceRewrite,
  useIntelligenceStreamRewrite,
} from "../index";
import type { RewriteRequest } from "../index";

describe("generated Intelligence client", () => {
  it("keeps the live transcription capability route stable", () => {
    expect(getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey()).toEqual([
      "/api/modules/intelligence/transcription/live/capabilities",
    ]);
  });

  it("exports the rewrite mutation surface from the shared package", () => {
    const request: RewriteRequest = { prompt: "Make this clearer." };

    expect(request.prompt).toBe("Make this clearer.");
    expect(typeof intelligenceRewrite).toBe("function");
    expect(typeof getIntelligenceRewriteMutationOptions).toBe("function");
    expect(typeof useIntelligenceRewrite).toBe("function");
    expect(typeof intelligenceStreamRewrite).toBe("function");
    expect(typeof getIntelligenceStreamRewriteMutationOptions).toBe("function");
    expect(typeof useIntelligenceStreamRewrite).toBe("function");
  });
});
