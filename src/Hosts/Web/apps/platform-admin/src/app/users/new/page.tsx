"use client";

import { UserManagementSurface } from "@vitarish/users";
import { useLocalizedText } from "@vitarish/localization";

export default function NewUserPage() {
  const { t } = useLocalizedText();
  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <UserManagementSurface mode="platform" platformView="create" title={t("admin.users.createTitle")} />
    </main>
  );
}
