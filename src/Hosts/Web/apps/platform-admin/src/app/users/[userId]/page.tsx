"use client";

import { Routes } from "../../routes";
import { useRouter, useParams } from "next/navigation";
import { UserManagementSurface } from "@vitarish/users";
import { useLocalizedText } from "@vitarish/localization";

export default function UserDetailPage() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { userId } = useParams<{ userId: string }>();
  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <UserManagementSurface mode="platform" platformView="detail" userId={userId} onUserDeleted={() => router.replace(Routes.Users.Index)} title={t("admin.users.detailTitle")} />
    </main>
  );
}
