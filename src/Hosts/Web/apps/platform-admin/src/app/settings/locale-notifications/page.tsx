"use client";
import { LocaleNotificationListPage } from "@vitarish/platform";
import { usePlatformAuthorization } from "@vitarish/auth";
export default function LocaleNotificationsPage() { const { hasAccess } = usePlatformAuthorization(); if (!hasAccess) return null; return <main className="mx-auto max-w-6xl p-6"><LocaleNotificationListPage basePath="/settings/locale-notifications" /></main>; }
