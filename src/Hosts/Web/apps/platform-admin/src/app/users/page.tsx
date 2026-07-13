"use client";

import { PlatformAdminGuard } from "@/components/guards/PlatformAdminGuard";
import { useLocalizedText } from "@/localization";

export default function UsersPage() {
  const { t } = useLocalizedText();

  return (
    <PlatformAdminGuard>
      <div className="p-6">
        <h1 className="text-2xl font-bold">{t("platformAdmin.nav.users")}</h1>
      </div>
    </PlatformAdminGuard>
  );
}
