"use client";

import { UserMemorySection } from "@vitarish/intelligence";
import { PageHeader } from "@vitarish/ui";
import { AccountBackLink, AccountSurface } from "@/components/account/AccountSurface";
import { useLocalizedText } from "@/localization";

/** What Vitarish remembers about the signed-in person, which they can correct or delete (ADR-0017). */
export default function AccountMemoryPage() {
  const { t } = useLocalizedText();

  return (
    <AccountSurface>
      <div className="space-y-6">
        <AccountBackLink />
        <PageHeader icon="brain" title={t("account.memory.title")} details={t("account.memory.description")} />
        <UserMemorySection />
      </div>
    </AccountSurface>
  );
}
