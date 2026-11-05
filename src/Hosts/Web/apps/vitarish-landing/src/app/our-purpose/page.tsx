import type { Metadata } from "next";
import { headers } from "next/headers";
import { resolveI18nAppLocale } from "@vitarish/localization";
import {
  fetchPublishedEntry,
  fetchPublishedSite,
  localizedValue,
  RichText,
} from "@vitarish/contents/delivery";
import { apiProxyTargetEnvironmentKey } from "../../../../../scripts/api-proxy-rewrites.mjs";
import {
  LANDING_CONTENT_SITE_KEY,
  LANDING_EDITORIAL_PAGE_FIELDS,
  LANDING_EDITORIAL_PAGE_TYPE_KEY,
} from "../../content/constants";
import type { PurposePageContent } from "../../features/purpose/PurposePage";
import { PurposePage } from "../../features/purpose/PurposePage";
import { LandingRoutes } from "../routes";
import { catalogs } from "../../localization/messages";

export async function generateMetadata(): Promise<Metadata> {
  const headerStore = await headers();
  const { locale } = resolveI18nAppLocale({
    cookieHeader: headerStore.get("cookie"),
    acceptLanguageHeader: headerStore.get("accept-language"),
  });
  const text = catalogs[locale].landing.purpose;
  const content = await loadPurposePageContent(headerStore, locale);
  return {
    title: content?.title ?? text.title,
    description: content ? (content.seoDescription ?? undefined) : text.description,
  };
}

export default async function Page() {
  const headerStore = await headers();
  const { locale } = resolveI18nAppLocale({
    cookieHeader: headerStore.get("cookie"),
    acceptLanguageHeader: headerStore.get("accept-language"),
  });
  const content = await loadPurposePageContent(headerStore, locale);

  // Compatibility fallback: remove catalog copy after the landing editorial entry is seeded, published, and verified per ADR-0019 §8.
  return <PurposePage content={content} />;
}

// Published content is optional for this page: when delivery is unavailable the page keeps its catalog copy.
async function loadPurposePageContent(
  requestHeaders: Awaited<ReturnType<typeof headers>>,
  locale: string,
): Promise<PurposePageContent | null> {
  try {
    return await readPurposePageContent(requestHeaders, locale);
  } catch (error) {
    console.error("Published purpose page content is unavailable; using catalog copy.", error);
    return null;
  }
}

async function readPurposePageContent(
  requestHeaders: Awaited<ReturnType<typeof headers>>,
  locale: string,
): Promise<PurposePageContent | null> {
  const tenantHost = requestHeaders.get("host");
  if (!tenantHost) throw new Error("The landing request did not include a host.");

  const apiTarget = process.env[apiProxyTargetEnvironmentKey]?.trim();
  if (!apiTarget) {
    throw new Error(`${apiProxyTargetEnvironmentKey} must be configured for public content delivery.`);
  }
  const apiBaseUrl = new URL(apiTarget).origin;
  const request = {
    apiBaseUrl,
    tenantHost,
    siteKey: LANDING_CONTENT_SITE_KEY,
  };
  const [site, entry] = await Promise.all([
    fetchPublishedSite(request),
    fetchPublishedEntry({ ...request, path: LandingRoutes.Purpose }),
  ]);

  if (!site || !entry) return null;
  if (entry.typeKey !== LANDING_EDITORIAL_PAGE_TYPE_KEY) {
    throw new Error("The landing purpose entry has an unexpected content type.");
  }

  const localizedField = (key: string) =>
    localizedValue(entry.fields[key], locale, site.defaultLocale);
  const localizedTextField = (key: string) =>
    toText(localizedField(key));
  const category = localizedTextField(LANDING_EDITORIAL_PAGE_FIELDS.category);
  const seoDescription = localizedTextField(LANDING_EDITORIAL_PAGE_FIELDS.seoDescription) ?? entry.excerpt;

  return {
    title: localizedTextField(LANDING_EDITORIAL_PAGE_FIELDS.title) ?? entry.title,
    category,
    body: <RichText content={localizedField(LANDING_EDITORIAL_PAGE_FIELDS.body)} />,
    seoDescription,
  };
}

function toText(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}
