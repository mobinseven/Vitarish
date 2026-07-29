import { beforeEach, describe, expect, it, vi } from "vitest";
import { intelligenceRewrite, useIntelligenceRewrite } from "../api/generated/intelligence/intelligence";
import { rewrite, useRewrite } from "../hooks/useRewrite";

vi.mock("../api/generated/intelligence/intelligence", () => ({
  intelligenceRewrite: vi.fn(),
  useIntelligenceRewrite: vi.fn(),
}));

const mockedRewrite = vi.mocked(intelligenceRewrite);
const mockedUseRewrite = vi.mocked(useIntelligenceRewrite);

describe("rewrite facade", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the typed rewrite result and forwards cancellation", async () => {
    const controller = new AbortController();
    mockedRewrite.mockResolvedValue({ text: "Clearer text." });

    await expect(
      rewrite({ prompt: "Improve this." }, undefined, controller.signal),
    ).resolves.toEqual({ text: "Clearer text." });
    expect(mockedRewrite).toHaveBeenCalledWith(
      { prompt: "Improve this." },
      undefined,
      controller.signal,
    );
  });

  it("delegates React Query configuration to the generated hook", () => {
    const options = { mutation: { retry: false } };
    const mutation = { mutate: vi.fn() };
    mockedUseRewrite.mockReturnValue(mutation as never);

    expect(useRewrite(options)).toBe(mutation);
    expect(mockedUseRewrite).toHaveBeenCalledWith(options);
  });
});
