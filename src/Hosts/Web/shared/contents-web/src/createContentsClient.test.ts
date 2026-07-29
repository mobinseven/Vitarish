import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiFetch } from "@vitarish/web-api-client";
import { createContentsClient } from "./createContentsClient";

vi.mock("@vitarish/web-api-client", () => ({ apiFetch: vi.fn() }));
const fetchMock = vi.mocked(apiFetch);

describe("createContentsClient", () => {
  beforeEach(() => fetchMock.mockReset());

  it("uses the protected content collection for list and create", async () => {
    fetchMock.mockResolvedValue([]);
    const client = createContentsClient();
    await client.list();
    await client.create({
      type: "Article",
      status: "Draft",
      slug: "welcome",
      canonicalPath: "/welcome",
      title: "Welcome",
      excerpt: "",
      body: [],
      locale: "en",
    });
    expect(fetchMock).toHaveBeenNthCalledWith(1, "/api/contents/items", {
      signal: undefined,
    });
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/contents/items",
      expect.objectContaining({ method: "POST" }),
    );
  });

  it("encodes identifiers for item workflows", async () => {
    fetchMock.mockResolvedValue({});
    const client = createContentsClient();
    await client.archive("item/id");
    await client.revisions("item/id");
    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      "/api/contents/items/item%2Fid/archive",
      { method: "POST" },
    );
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/contents/items/item%2Fid/revisions",
      { signal: undefined },
    );
  });
});
