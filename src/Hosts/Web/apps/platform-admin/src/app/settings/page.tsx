"use client";
import Link from "next/link";
import { usePlatformAuthorization } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
export default function SettingsPage() { const { hasAccess } = usePlatformAuthorization(); const { t } = useLocalizedText(); if (!hasAccess) return null; return <main className="mx-auto max-w-4xl space-y-4 p-6"><h1 className="text-2xl font-semibold">{t("admin.settingsTitle")}</h1><Link className="block underline" href="/settings/landing">{t("admin.landing.title")}</Link><Link className="block underline" href="/settings/locale-notifications">{t("admin.localeNotificationsLink")}</Link></main>; }
