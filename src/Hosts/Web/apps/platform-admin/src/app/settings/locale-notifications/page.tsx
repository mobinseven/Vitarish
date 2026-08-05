"use client";
import { LocaleNotificationListPage } from "@vitarish/platform-web";
import { usePlatformAuthorization } from "@vitarish/auth-web";
export default function LocaleNotificationsPage() { const { hasAccess } = usePlatformAuthorization(); if (!hasAccess) return null; return <main className="mx-auto max-w-6xl p-6"><LocaleNotificationListPage basePath="/settings/locale-notifications" /></main>; }
