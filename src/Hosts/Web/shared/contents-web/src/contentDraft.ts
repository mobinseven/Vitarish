import type { ContentItem, ContentItemInput } from "./types";

export const blankContentDraft: ContentItemInput = {
  type: "Article",
  status: "Draft",
  slug: "",
  canonicalPath: "",
  title: "",
  excerpt: "",
  body: [{ type: "paragraph", text: "" }],
  locale: "en",
};

export function contentDraftFromItem(item: ContentItem): ContentItemInput {
  return {
    type: item.type,
    status: item.status,
    slug: item.slug,
    canonicalPath: item.canonicalPath,
    title: item.title,
    excerpt: item.excerpt,
    body: item.body.length ? item.body : blankContentDraft.body,
    locale: item.locale,
  };
}

export function updatePrimaryBodyText(
  body: ContentItemInput["body"],
  text: string,
): ContentItemInput["body"] {
  return body.map((block, index) =>
    index === 0 ? { ...block, text } : block,
  );
}
