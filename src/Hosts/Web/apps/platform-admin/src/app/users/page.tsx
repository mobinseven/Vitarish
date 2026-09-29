"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { UserManagementSurface } from "@vitarish/users";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "../routes";

export default function UsersPage() {
  const { t } = useLocalizedText();
  const router = useRouter();
  return (
    <main className="mx-auto max-w-6xl space-y-6 p-6">
      <div className="flex justify-end">
        <Link className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" href={Routes.Users.New} title={t("admin.users.createTitle")}>
          <i aria-hidden="true" className="fa-light fa-user-plus" /> {t("admin.users.createTitle")}
        </Link>
      </div>
      <UserManagementSurface
        mode="platform"
        title={t("admin.users.title")}
        description={t("admin.users.description")}
        onOpenUserDetails={(userId) => router.push(Routes.Users.Detail(userId))}
      />
    </main>
  );
}
