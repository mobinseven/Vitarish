"use client";

import { UserMemorySection } from "@vitarish/intelligence";
import { HeaderIcon } from "@vitarish/ui";
import { AccountBackLink, AccountSurface } from "@/components/account/AccountSurface";
import { useLocalizedText } from "@/localization";

/** What Vitarish remembers about the signed-in person, which they can correct or delete (ADR-0017). */
export default function AccountMemoryPage() {
  const { t } = useLocalizedText();

  return (
    <AccountSurface>
      <div className="space-y-6">
        <AccountBackLink />
        <div className="flex items-center gap-4">
          <HeaderIcon icon="brain" level="page" />
          <div className="min-w-0">
            <h1 className="text-xl font-semibold text-foreground">{t("account.memory.title")}</h1>
            <p className="mt-2 text-sm text-foreground-muted">{t("account.memory.description")}</p>
          </div>
        </div>
        <UserMemorySection />
      </div>
    </AccountSurface>
  );
}
