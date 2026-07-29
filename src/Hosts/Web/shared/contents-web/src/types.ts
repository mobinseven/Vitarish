export type ContentItemType =
  | "Article"
  | "BlogPost"
  | "Announcement"
  | "Page"
  | "Guide";
export type ContentStatus = "Draft" | "Scheduled" | "Published" | "Archived";
export interface ContentBodyBlock {
  type: string;
  text?: string | null;
  assetId?: string | null;
}
export interface ContentItemInput {
  type: ContentItemType;
  status: ContentStatus;
  slug: string;
  canonicalPath: string;
  title: string;
  excerpt: string;
  body: ContentBodyBlock[];
  locale: string;
}
export interface ContentItem extends ContentItemInput {
  id: string;
  publishedAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}
export interface ContentRevision {
  id: string;
  version: number;
  createdAt: string;
}
export interface ContentsClient {
  list(signal?: AbortSignal): Promise<ContentItem[]>;
  get(id: string, signal?: AbortSignal): Promise<ContentItem>;
  create(input: ContentItemInput): Promise<ContentItem>;
  update(id: string, input: ContentItemInput): Promise<ContentItem>;
  archive(id: string): Promise<ContentItem>;
  revisions(id: string, signal?: AbortSignal): Promise<ContentRevision[]>;
}
