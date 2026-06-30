import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, apiFetch, resetAntiforgeryToken } from "@/api/client/apiFetch";

function jsonResponse(body: unknown, init: ResponseInit = {}) {
  return new Response(JSON.stringify(body), {
    status: init.status ?? 200,
    headers: { "Content-Type": "application/json" },
    ...init,
  });
}

describe("apiFetch antiforgery", () => {
  beforeEach(() => {
    resetAntiforgeryToken();
    vi.restoreAllMocks();
  });

  it("fetches an antiforgery token before unsafe requests", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: "token" }))
      .mockResolvedValueOnce(jsonResponse({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/modules/example", { method: "POST", body: JSON.stringify({}) });

    expect(fetchMock.mock.calls[0][0]).toBe("https://localhost:7272/api/modules/authentication/antiforgery");
    expect(fetchMock.mock.calls[1][0]).toBe("https://localhost:7272/api/modules/example");
  });

  it("rejects the original unsafe request when bootstrap fails", async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse({ title: "No token" }, { status: 500 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiFetch("/api/modules/example", { method: "POST" })).rejects.toBeInstanceOf(ApiError);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("rejects malformed antiforgery responses", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValueOnce(jsonResponse({ headerName: "X-XSRF-TOKEN" })));

    await expect(apiFetch("/api/modules/example", { method: "DELETE" })).rejects.toMatchObject({ code: "AntiforgeryBootstrapFailed" });
  });

  it("sets X-XSRF-TOKEN when bootstrap succeeds", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: "abc" }))
      .mockResolvedValueOnce(jsonResponse({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/modules/example", { method: "PUT", body: "{}" });

    expect(fetchMock.mock.calls[1][1].headers.get("X-XSRF-TOKEN")).toBe("abc");
  });

  it("shares one bootstrap call across concurrent unsafe requests", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: "shared" }))
      .mockImplementation(() => Promise.resolve(jsonResponse({ ok: true })));
    vi.stubGlobal("fetch", fetchMock);

    await Promise.all([
      apiFetch("/api/modules/one", { method: "POST" }),
      apiFetch("/api/modules/two", { method: "POST" }),
    ]);

    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes("/antiforgery"))).toHaveLength(1);
  });

  it("retries exactly once for confirmed antiforgery validation failures", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: "old" }))
      .mockResolvedValueOnce(jsonResponse({ code: "AntiforgeryValidationFailed", status: 400 }, { status: 400 }))
      .mockResolvedValueOnce(jsonResponse({ requestToken: "new" }))
      .mockResolvedValueOnce(jsonResponse({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/modules/example", { method: "POST", body: "{}" });

    expect(fetchMock).toHaveBeenCalledTimes(4);
    expect(fetchMock.mock.calls[3][1].headers.get("X-XSRF-TOKEN")).toBe("new");
  });

  it("does not retry non-antiforgery API failures", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: "token" }))
      .mockResolvedValueOnce(jsonResponse({ code: "ValidationFailed", status: 400 }, { status: 400 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiFetch("/api/modules/example", { method: "POST" })).rejects.toMatchObject({ code: "ValidationFailed" });
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
