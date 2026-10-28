import type { Metadata } from "next";
import { headers } from "next/headers";
import { PracticeOverviewPageWrapper } from "@/features/landing/PracticeOverviewPageWrapper";
import { resolveI18nAppLocale } from "@/localization";
import { catalogs } from "@/localization/messages";

export async function generateMetadata(): Promise<Metadata> {
  const headerStore = await headers();
  const { locale } = resolveI18nAppLocale({ cookieHeader: headerStore.get("cookie"), acceptLanguageHeader: headerStore.get("accept-language") });
  const text = catalogs[locale].practiceLanding;
  return { title: text.editorial.title, description: text.description };
}

export default function Page() {
  return <PracticeOverviewPageWrapper />;
}
