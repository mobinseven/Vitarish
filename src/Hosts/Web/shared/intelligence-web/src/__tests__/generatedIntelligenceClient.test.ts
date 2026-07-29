import { describe, expect, it } from "vitest";
import {
  getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey,
} from "../api/generated/intelligence/intelligence";
import {
  rewrite,
  useRewrite,
} from "../index";
import type { RewriteRequest, RewriteResult } from "../index";

describe("generated Intelligence client", () => {
  it("keeps the live transcription capability route stable", () => {
    expect(getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey()).toEqual([
      "/api/modules/intelligence/transcription/live/capabilities",
    ]);
  });

  it("exports the feature-owned rewrite surface from the shared package", () => {
    const request: RewriteRequest = { prompt: "Make this clearer." };
    const result: RewriteResult = { text: "This is clearer." };

    expect(request.prompt).toBe("Make this clearer.");
    expect(result.text).toBe("This is clearer.");
    expect(typeof rewrite).toBe("function");
    expect(typeof useRewrite).toBe("function");
  });
});
