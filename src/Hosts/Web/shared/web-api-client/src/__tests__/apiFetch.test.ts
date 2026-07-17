import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, apiFetch, apiFetchBlob, resetAntiforgeryToken } from "../apiFetch";

const tenantHostHeader = "X-Vitarish-Tenant-Host";

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

  it("forwards the current web host as tenant context on safe requests", async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/modules/example");

    expect(fetchMock.mock.calls[0][1].headers.get(tenantHostHeader)).toBe("tenant-a.example.test");
  });

  it("forwards cancellation signals to fetch", async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(jsonResponse({ ok: true }));
    const controller = new AbortController();
    const originalRequest = globalThis.Request;
    vi.stubGlobal("fetch", fetchMock);

    vi.stubGlobal(
      "Request",
      function AcceptingRequest() {} as unknown as typeof Request
    );

    try {
      await apiFetch("/api/modules/example", { signal: controller.signal });

      expect(fetchMock.mock.calls[0][1].signal).toBe(controller.signal);
    } finally {
      vi.stubGlobal("Request", originalRequest);
    }
  });

  it("fetches an antiforgery token before unsafe requests", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: "token" }))
      .mockResolvedValueOnce(jsonResponse({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await apiFetch("/api/modules/example", { method: "POST", body: JSON.stringify({}) });

    expect(fetchMock.mock.calls[0][0]).toBe("https://localhost:7272/api/modules/authentication/antiforgery");
    expect(fetchMock.mock.calls[1][0]).toBe("https://localhost:7272/api/modules/example");
    expect(fetchMock.mock.calls[0][1].headers.get(tenantHostHeader)).toBe("tenant-a.example.test");
    expect(fetchMock.mock.calls[1][1].headers.get(tenantHostHeader)).toBe("tenant-a.example.test");
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
    expect(fetchMock.mock.calls[1][1].headers.get(tenantHostHeader)).toBe("tenant-a.example.test");
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

describe("apiFetchBlob", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("downloads authenticated tenant-scoped binary content", async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(new Response("image", {
      headers: { "Content-Type": "image/png" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    const result = await apiFetchBlob("/api/modules/files/file-id/content");

    expect(result).toMatchObject({ size: 5, type: "image/png" });
    expect(fetchMock.mock.calls[0][0]).toBe(
      "https://localhost:7272/api/modules/files/file-id/content",
    );
    expect(fetchMock.mock.calls[0][1].credentials).toBe("include");
    expect(fetchMock.mock.calls[0][1].headers.get(tenantHostHeader)).toBe(
      "tenant-a.example.test",
    );
  });

  it("maps binary download failures to ApiError", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValueOnce(
        jsonResponse({ status: 404, code: "NotFound", title: "Missing" }, { status: 404 }),
      ),
    );

    await expect(apiFetchBlob("/api/modules/files/missing/content")).rejects.toMatchObject({
      status: 404,
      code: "NotFound",
    });
  });
});
