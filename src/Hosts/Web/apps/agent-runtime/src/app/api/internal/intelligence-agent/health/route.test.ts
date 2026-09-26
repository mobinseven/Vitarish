import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { GET } from "./route";

describe("agent runtime health route", () => {
  const secret = "local-health-secret";
  const healthUrl = "http://main/api/modules/clinic/internal/assistant/health";
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    vi.stubEnv("AGENT_RUNTIME_SHARED_SECRET", secret);
    vi.stubEnv("AGENT_RUNTIME_INTELLIGENCE_HEALTH_URL", healthUrl);
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
    fetchMock.mockReset();
  });

  it("rejects callers without the shared secret", async () => {
    const response = await GET(new NextRequest("http://localhost/health"));

    expect(response.status).toBe(401);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("forwards the internal credential header to Main and preserves its response", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 200 }));

    const response = await GET(new NextRequest("http://localhost/health", {
      headers: { authorization: `Bearer ${secret}` },
    }));

    expect(response.status).toBe(200);
    expect(await response.json()).toEqual({ status: "ok", intelligenceStatus: 200 });
    expect(fetchMock).toHaveBeenCalledWith(healthUrl, expect.objectContaining({
      headers: { "X-Vitarish-Agent-Credential": secret },
      cache: "no-store",
    }));
  });
});
