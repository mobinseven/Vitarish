import { describe, expect, it } from "vitest";
import {
  getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey,
} from "../api/generated/intelligence/intelligence";

describe("generated Intelligence client", () => {
  it("keeps the live transcription capability route stable", () => {
    expect(getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey()).toEqual([
      "/api/modules/intelligence/transcription/live/capabilities",
    ]);
  });
});
