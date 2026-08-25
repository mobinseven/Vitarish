import { describe, expect, it } from "vitest";
import { contentDraftFromItem, updatePrimaryBodyText } from "./contentDraft";
import type { ContentItem } from "./types";

describe("content draft editing", () => {
  it("keeps every existing body block when the primary text changes", () => {
    const body = [
      { type: "paragraph", text: "Before" },
      { type: "image", assetId: "4f61053c-8e47-4aa5-a32f-ad39de6aee0f" },
    ];

    expect(updatePrimaryBodyText(body, "After")).toEqual([
      { type: "paragraph", text: "After" },
      { type: "image", assetId: "4f61053c-8e47-4aa5-a32f-ad39de6aee0f" },
    ]);
  });

  it("copies only editable fields into the draft", () => {
    const item: ContentItem = {
      id: "content-id",
      type: "Article",
      status: "Published",
      slug: "welcome",
      canonicalPath: "/welcome",
      title: "Welcome",
      excerpt: "Excerpt",
      body: [],
      locale: "en",
      createdAt: "2026-07-01T12:00:00Z",
      publishedAt: "2026-07-01T12:00:00Z",
    };

    const draft = contentDraftFromItem(item);

    expect(draft).not.toHaveProperty("id");
    expect(draft).not.toHaveProperty("publishedAt");
    expect(draft.body).toEqual([{ type: "paragraph", text: "" }]);
  });
});
