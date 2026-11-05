// Replaced by generated client after spec regeneration.
import { apiFetch } from "@vitarish/api-client";
import {
  contentsApiBaseRoute,
  publishedEntriesQueryKeys,
} from "../contentsApiRoutes";

export const contentsStudioApiRoutes = {
  base: contentsApiBaseRoute,
  sites: "/sites",
  site: "/sites/{siteId:guid}",
  types: "/types",
  type: "/types/{typeId:guid}",
  typeSchema: "/types/{typeId:guid}/schema",
  entries: "/entries",
  entry: "/entries/{id:guid}",
  entryDraft: "/entries/{id:guid}/draft",
  entryPublish: "/entries/{id:guid}/publish",
  entryUnpublish: "/entries/{id:guid}/unpublish",
  entryArchive: "/entries/{id:guid}/archive",
  entryRestore: "/entries/{id:guid}/restore",
  entryRevisions: "/entries/{id:guid}/revisions",
  entryRevision: "/entries/{id:guid}/revisions/{revisionId:guid}",
} as const;

export const contentsStudioEntriesQueryKeys = {
  siteId: "siteId",
  typeId: "typeId",
  status: "status",
  skip: publishedEntriesQueryKeys.skip,
  take: publishedEntriesQueryKeys.take,
} as const;

export const contentFieldKind = {
  text: "Text",
  longText: "LongText",
  richText: "RichText",
  number: "Number",
  boolean: "Boolean",
  date: "Date",
  media: "Media",
  link: "Link",
  reference: "Reference",
  list: "List",
  group: "Group",
} as const;
export const contentFieldKinds = [
  contentFieldKind.text,
  contentFieldKind.longText,
  contentFieldKind.richText,
  contentFieldKind.number,
  contentFieldKind.boolean,
  contentFieldKind.date,
  contentFieldKind.media,
  contentFieldKind.link,
  contentFieldKind.reference,
  contentFieldKind.list,
  contentFieldKind.group,
] as const;
export type ContentFieldKind = (typeof contentFieldKinds)[number];

export const contentEntryStatuses = ["Draft", "Scheduled", "Published", "Archived"] as const;
export type ContentEntryStatus = (typeof contentEntryStatuses)[number];

export interface ContentFieldDefinition {
  key: string;
  label: string;
  kind: ContentFieldKind;
  required: boolean;
  localized: boolean;
  maxLength?: number | null;
  allowedTypeKeys?: string[] | null;
  item?: ContentFieldDefinition | null;
  fields?: ContentFieldDefinition[] | null;
}

export interface ContentSite {
  id: string;
  key: string;
  name: string;
  defaultLocale: string;
  locales: string[];
}

export interface ContentSiteRequest {
  key: string;
  name: string;
  defaultLocale: string;
  locales: string[];
}

export interface ContentType {
  id: string;
  key: string;
  name: string;
  description: string | null;
  currentSchemaVersion: number;
  fields: ContentFieldDefinition[];
}

export interface ContentTypeRequest {
  key: string;
  name: string;
  description: string | null;
  fields: ContentFieldDefinition[];
}

export interface ContentSchemaRequest {
  fields: ContentFieldDefinition[];
}

export type ContentJsonValue =
  | string
  | number
  | boolean
  | null
  | ContentJsonValue[]
  | { [key: string]: ContentJsonValue };

export type ContentEntryFields = Record<string, ContentJsonValue>;

export interface ContentEntry {
  id: string;
  siteId: string;
  contentTypeId: string;
  schemaVersion: number;
  slug: string;
  canonicalPath: string;
  title: string;
  excerpt: string | null;
  fields: ContentEntryFields;
  version: number;
  status: ContentEntryStatus;
  workingRevisionId: string | null;
  publishedRevisionId: string | null;
  publishedAt: string | null;
}

export interface ContentEntryRequest {
  siteId: string;
  contentTypeId: string;
  slug: string;
  canonicalPath: string;
  title: string;
  excerpt: string | null;
  fields: ContentEntryFields;
}

export interface DraftRequest {
  expectedVersion: number;
  slug: string;
  canonicalPath: string;
  title: string;
  excerpt: string | null;
  fields: ContentEntryFields;
}

export interface EntryVersionRequest {
  expectedVersion: number;
}

export interface RevisionActionRequest {
  revisionId: string;
  expectedVersion: number;
}

export interface ContentEntryOutcome {
  id: string;
  version?: number;
  revisionId?: string;
  status?: ContentEntryStatus;
}

export interface ContentRevision {
  id: string;
  version: number;
  schemaVersion: number;
  slug: string;
  canonicalPath: string;
  title: string;
  excerpt: string | null;
  fields: ContentEntryFields;
  createdBy: string | null;
  createdAt: string;
}

export interface ContentEntryFilter {
  siteId?: string;
  typeId?: string;
  status?: string;
  skip?: number;
  take?: number;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isString(value: unknown): value is string {
  return typeof value === "string";
}

function isNullableString(value: unknown): value is string | null {
  return value === null || isString(value);
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every(isString);
}

function isJsonValue(value: unknown, depth = 0): value is ContentJsonValue {
  if (depth > 64) return false;
  if (value === null || typeof value === "string" || typeof value === "boolean") return true;
  if (typeof value === "number") return Number.isFinite(value);
  if (Array.isArray(value)) return value.every((item) => isJsonValue(item, depth + 1));
  return isRecord(value) && Object.values(value).every((item) => isJsonValue(item, depth + 1));
}

function isContentFields(value: unknown): value is ContentEntryFields {
  return isRecord(value) && Object.values(value).every((item) => isJsonValue(item));
}

function isContentFieldDefinition(
  value: unknown,
  depth = 0,
): value is ContentFieldDefinition {
  if (depth > 8 || !isRecord(value)) return false;
  const item = value.item;
  const fields = value.fields;
  const maxLength = value.maxLength;
  const allowedTypeKeys = value.allowedTypeKeys;
  return isString(value.key) &&
    isString(value.label) &&
    typeof value.kind === "string" &&
    (contentFieldKinds as readonly string[]).includes(value.kind) &&
    typeof value.required === "boolean" &&
    typeof value.localized === "boolean" &&
    (maxLength === undefined || maxLength === null || Number.isInteger(maxLength)) &&
    (allowedTypeKeys === undefined || allowedTypeKeys === null || isStringArray(allowedTypeKeys)) &&
    (item === undefined || item === null || isContentFieldDefinition(item, depth + 1)) &&
    (fields === undefined || fields === null || Array.isArray(fields) && fields.every((field) => isContentFieldDefinition(field, depth + 1)));
}

function isContentSite(value: unknown): value is ContentSite {
  return isRecord(value) && isString(value.id) && isString(value.key) &&
    isString(value.name) && isString(value.defaultLocale) && isStringArray(value.locales);
}

function isContentType(value: unknown): value is ContentType {
  return isRecord(value) && isString(value.id) && isString(value.key) &&
    isString(value.name) && isNullableString(value.description) &&
    typeof value.currentSchemaVersion === "number" &&
    Number.isInteger(value.currentSchemaVersion) && Array.isArray(value.fields) &&
    value.fields.every((field) => isContentFieldDefinition(field));
}

function isContentEntry(value: unknown): value is ContentEntry {
  return isRecord(value) && isString(value.id) && isString(value.siteId) &&
    isString(value.contentTypeId) && typeof value.schemaVersion === "number" &&
    Number.isInteger(value.schemaVersion) && isString(value.slug) &&
    isString(value.canonicalPath) && isString(value.title) &&
    isNullableString(value.excerpt) && isContentFields(value.fields) &&
    typeof value.version === "number" && Number.isInteger(value.version) &&
    typeof value.status === "string" &&
    (contentEntryStatuses as readonly string[]).includes(value.status) &&
    isNullableString(value.workingRevisionId) &&
    isNullableString(value.publishedRevisionId) && isNullableString(value.publishedAt);
}

function isContentEntryOutcome(value: unknown): value is ContentEntryOutcome {
  return isRecord(value) && isString(value.id) &&
    (value.version === undefined || typeof value.version === "number" && Number.isInteger(value.version)) &&
    (value.revisionId === undefined || isString(value.revisionId)) &&
    (value.status === undefined || typeof value.status === "string" && (contentEntryStatuses as readonly string[]).includes(value.status));
}

function isContentRevision(value: unknown): value is ContentRevision {
  return isRecord(value) && isString(value.id) && typeof value.version === "number" &&
    Number.isInteger(value.version) && typeof value.schemaVersion === "number" &&
    Number.isInteger(value.schemaVersion) && isString(value.slug) &&
    isString(value.canonicalPath) && isString(value.title) &&
    isNullableString(value.excerpt) && isContentFields(value.fields) &&
    isNullableString(value.createdBy) && isString(value.createdAt);
}

function isArrayOf<T>(value: unknown, guard: (item: unknown) => item is T): value is T[] {
  return Array.isArray(value) && value.every(guard);
}

async function apiFetchValidated<T>(
  url: string,
  guard: (value: unknown) => value is T,
  options?: RequestInit,
): Promise<T> {
  const response = await apiFetch<unknown>(url, options);
  if (!guard(response)) throw new TypeError("The Contents API returned an invalid response.");
  return response;
}

function bindRoute(
  route: string,
  parameters: Readonly<Record<string, string>>,
): string {
  return route.replace(/\{([A-Za-z]+):guid\}/g, (placeholder, name: string) => {
    const value = parameters[name];
    if (!value) throw new Error(`Missing route parameter for ${placeholder}.`);
    return encodeURIComponent(value);
  });
}

function apiPath(route: string): string {
  return `${contentsStudioApiRoutes.base}${route}`;
}

function entryRoute(route: string, id: string): string {
  return apiPath(bindRoute(route, { id }));
}

function entriesPath(filter?: ContentEntryFilter): string {
  const query = new URLSearchParams();
  if (filter?.siteId) query.set(contentsStudioEntriesQueryKeys.siteId, filter.siteId);
  if (filter?.typeId) query.set(contentsStudioEntriesQueryKeys.typeId, filter.typeId);
  if (filter?.status) query.set(contentsStudioEntriesQueryKeys.status, filter.status);
  if (filter?.skip !== undefined) query.set(contentsStudioEntriesQueryKeys.skip, String(filter.skip));
  if (filter?.take !== undefined) query.set(contentsStudioEntriesQueryKeys.take, String(filter.take));
  const queryString = query.toString();
  return `${apiPath(contentsStudioApiRoutes.entries)}${queryString ? `?${queryString}` : ""}`;
}

export function listContentSites(signal?: AbortSignal): Promise<ContentSite[]> {
  return apiFetchValidated(apiPath(contentsStudioApiRoutes.sites), (value): value is ContentSite[] => isArrayOf(value, isContentSite), { signal });
}

export function createContentSite(input: ContentSiteRequest): Promise<ContentEntryOutcome> {
  return apiFetchValidated(apiPath(contentsStudioApiRoutes.sites), isContentEntryOutcome, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateContentSite(
  id: string,
  input: ContentSiteRequest,
): Promise<ContentEntryOutcome> {
  return apiFetchValidated(apiPath(bindRoute(contentsStudioApiRoutes.site, { siteId: id })), isContentEntryOutcome, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function listContentTypes(signal?: AbortSignal): Promise<ContentType[]> {
  return apiFetchValidated(apiPath(contentsStudioApiRoutes.types), (value): value is ContentType[] => isArrayOf(value, isContentType), { signal });
}

export function getContentType(
  id: string,
  signal?: AbortSignal,
): Promise<ContentType> {
  return apiFetchValidated(apiPath(bindRoute(contentsStudioApiRoutes.type, { typeId: id })), isContentType, {
    signal,
  });
}

export function createContentType(input: ContentTypeRequest): Promise<ContentEntryOutcome> {
  return apiFetchValidated(apiPath(contentsStudioApiRoutes.types), isContentEntryOutcome, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateContentTypeSchema(
  id: string,
  input: ContentSchemaRequest,
): Promise<ContentEntryOutcome> {
  return apiFetchValidated(apiPath(bindRoute(contentsStudioApiRoutes.typeSchema, { typeId: id })), isContentEntryOutcome, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function listContentEntries(
  filter?: ContentEntryFilter,
  signal?: AbortSignal,
): Promise<ContentEntry[]> {
  return apiFetchValidated(entriesPath(filter), (value): value is ContentEntry[] => isArrayOf(value, isContentEntry), { signal });
}

export function createContentEntry(input: ContentEntryRequest): Promise<ContentEntryOutcome> {
  return apiFetchValidated(apiPath(contentsStudioApiRoutes.entries), isContentEntryOutcome, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function getContentEntry(
  id: string,
  signal?: AbortSignal,
): Promise<ContentEntry> {
  return apiFetchValidated(entryRoute(contentsStudioApiRoutes.entry, id), isContentEntry, { signal });
}

export function saveContentDraft(
  id: string,
  input: DraftRequest,
): Promise<ContentEntryOutcome> {
  return apiFetchValidated(entryRoute(contentsStudioApiRoutes.entryDraft, id), isContentEntryOutcome, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function publishContentEntry(
  id: string,
  input: RevisionActionRequest,
): Promise<ContentEntryOutcome> {
  return apiFetchValidated(entryRoute(contentsStudioApiRoutes.entryPublish, id), isContentEntryOutcome, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function unpublishContentEntry(
  id: string,
  input: EntryVersionRequest,
): Promise<ContentEntryOutcome> {
  return apiFetchValidated(entryRoute(contentsStudioApiRoutes.entryUnpublish, id), isContentEntryOutcome, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function archiveContentEntry(
  id: string,
  input: EntryVersionRequest,
): Promise<ContentEntryOutcome> {
  return apiFetchValidated(entryRoute(contentsStudioApiRoutes.entryArchive, id), isContentEntryOutcome, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function restoreContentRevision(
  id: string,
  input: RevisionActionRequest,
): Promise<ContentEntryOutcome> {
  return apiFetchValidated(entryRoute(contentsStudioApiRoutes.entryRestore, id), isContentEntryOutcome, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function listContentRevisions(
  id: string,
  signal?: AbortSignal,
): Promise<ContentRevision[]> {
  return apiFetchValidated(entryRoute(contentsStudioApiRoutes.entryRevisions, id), (value): value is ContentRevision[] => isArrayOf(value, isContentRevision), { signal });
}

export function getContentRevision(
  id: string,
  revisionId: string,
  signal?: AbortSignal,
): Promise<ContentRevision> {
  return apiFetchValidated(apiPath(bindRoute(contentsStudioApiRoutes.entryRevision, { id, revisionId })), isContentRevision, { signal });
}
