import {
  contentsPublicDeliveryRoutes,
  contentsTenantHostHeader,
  publicEntryPathQueryKey,
  publishedEntriesQueryKeys,
} from "./contentsApiRoutes";

export interface PublishedSite {
  id: string;
  key: string;
  name: string;
  defaultLocale: string;
  locales: string[];
}

export interface PublishedEntry {
  id: string;
  typeKey: string;
  slug: string;
  canonicalPath: string;
  title: string;
  excerpt: string | null;
  fields: Readonly<Record<string, unknown>>;
  publishedAt: string | null;
}

export interface PublishedContentRequest {
  apiBaseUrl: string;
  tenantHost: string;
  siteKey: string;
}

export interface PublishedEntryRequest extends PublishedContentRequest {
  path: string;
}

export interface PublishedEntriesRequest extends PublishedContentRequest {
  typeKey: string;
  skip?: number;
  take?: number;
}

export async function fetchPublishedSite(
  request: PublishedContentRequest,
): Promise<PublishedSite | null> {
  const route = contentsPublicDeliveryRoutes.site.replace("{siteKey}", encodeURIComponent(request.siteKey));
  return fetchPublishedResponse(
    request,
    `${contentsPublicDeliveryRoutes.base}${route}`,
    isPublishedSite,
    "published site",
  );
}

export async function fetchPublishedEntry(
  request: PublishedEntryRequest,
): Promise<PublishedEntry | null> {
  const route = contentsPublicDeliveryRoutes.entry.replace("{siteKey}", encodeURIComponent(request.siteKey));
  const query = new URLSearchParams({ [publicEntryPathQueryKey]: request.path });
  return fetchPublishedResponse(
    request,
    `${contentsPublicDeliveryRoutes.base}${route}?${query.toString()}`,
    isPublishedEntry,
    "published entry",
  );
}

export async function fetchPublishedEntries(
  request: PublishedEntriesRequest,
): Promise<PublishedEntry[] | null> {
  const route = contentsPublicDeliveryRoutes.entries
    .replace("{siteKey}", encodeURIComponent(request.siteKey))
    .replace("{typeKey}", encodeURIComponent(request.typeKey));
  const query = new URLSearchParams();
  if (request.skip !== undefined) query.set(publishedEntriesQueryKeys.skip, String(request.skip));
  if (request.take !== undefined) query.set(publishedEntriesQueryKeys.take, String(request.take));

  const queryString = query.toString();
  return fetchPublishedResponse(
    request,
    `${contentsPublicDeliveryRoutes.base}${route}${queryString ? `?${queryString}` : ""}`,
    isPublishedEntryList,
    "published entries",
  );
}

async function fetchPublishedResponse<T>(
  request: PublishedContentRequest,
  route: string,
  isResponse: (value: unknown) => value is T,
  responseName: string,
): Promise<T | null> {
  const baseUrl = request.apiBaseUrl.replace(/\/+$/, "");
  const response = await fetch(`${baseUrl}${route}`, {
    headers: { [contentsTenantHostHeader]: request.tenantHost },
    next: { revalidate: 60 },
  } as RequestInit & { next: { revalidate: number } });

  if (response.status === 404) return null;
  if (!response.ok) {
    throw new Error(`The ${responseName} request failed with status ${response.status}.`);
  }

  let payload: unknown;
  try {
    payload = await response.json();
  } catch {
    throw new Error(`The ${responseName} response was not valid JSON.`);
  }

  if (!isResponse(payload)) {
    throw new Error(`The ${responseName} response had an unexpected shape.`);
  }
  return payload;
}

export function localizedValue(
  field: unknown,
  locale: string,
  defaultLocale: string,
): unknown {
  if (!isRecord(field)) return field;
  const hasLocaleValue = Object.prototype.hasOwnProperty.call(field, locale);
  const hasDefaultLocaleValue = Object.prototype.hasOwnProperty.call(field, defaultLocale);
  if (!hasLocaleValue && !hasDefaultLocaleValue) return field;
  return field[locale] ?? field[defaultLocale];
}

function isPublishedSite(value: unknown): value is PublishedSite {
  return (
    isRecord(value) &&
    isString(value.id) &&
    isString(value.key) &&
    isString(value.name) &&
    isString(value.defaultLocale) &&
    Array.isArray(value.locales) &&
    value.locales.every(isString)
  );
}

function isPublishedEntry(value: unknown): value is PublishedEntry {
  return (
    isRecord(value) &&
    isString(value.id) &&
    isString(value.typeKey) &&
    isString(value.slug) &&
    isString(value.canonicalPath) &&
    isString(value.title) &&
    isNullableString(value.excerpt) &&
    isRecord(value.fields) &&
    isNullableString(value.publishedAt)
  );
}

function isPublishedEntryList(value: unknown): value is PublishedEntry[] {
  return Array.isArray(value) && value.every(isPublishedEntry);
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

export { RichText } from "./RichText";
export type { RichTextProps } from "./RichText";
