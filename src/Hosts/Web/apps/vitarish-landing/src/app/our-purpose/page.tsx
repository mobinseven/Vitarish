import type { Metadata } from "next";
import { headers } from "next/headers";
import { resolveI18nAppLocale } from "@vitarish/localization";
import { PurposePage } from "../../features/purpose/PurposePage";
import { catalogs } from "../../localization/messages";

export async function generateMetadata(): Promise<Metadata> {
  const headerStore = await headers();
  const { locale } = resolveI18nAppLocale({ cookieHeader: headerStore.get("cookie"), acceptLanguageHeader: headerStore.get("accept-language") });
  const text = catalogs[locale].landing.purpose;
  return { title: text.title, description: text.description };
}

export default function Page() {
  return <PurposePage />;
}
