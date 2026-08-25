"use client";
import { useParams } from "next/navigation";
import { LocaleNotificationEditorPage } from "@vitarish/platform";
import { usePlatformAuthorization } from "@vitarish/auth";
export default function LocaleNotificationPage() { const { hasAccess } = usePlatformAuthorization(); const { locale } = useParams<{ locale: string }>(); if (!hasAccess) return null; return <main className="mx-auto max-w-6xl p-6"><LocaleNotificationEditorPage normalizedLocale={locale} basePath="/settings/locale-notifications" /></main>; }
