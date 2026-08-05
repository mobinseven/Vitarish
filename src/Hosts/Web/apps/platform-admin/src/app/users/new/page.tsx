"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession, usePlatformAuthorization, useUsersAdministrationCreateWithEmail, useUsersAdministrationCreateWithPhone } from "@vitarish/auth-web";
import { CenteredRegion } from "@vitarish/web-ui/layout";
import { useLocalizedText } from "@vitarish/web-localization";

export default function NewUserPage() {
  const { isAuthenticated, isLoading } = useAuthSession();
  const { hasAccess } = usePlatformAuthorization();
  const { t } = useLocalizedText();
  const router = useRouter();
  const [mode, setMode] = useState<"email" | "phone">("email");
  const [contact, setContact] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const email = useUsersAdministrationCreateWithEmail();
  const phone = useUsersAdministrationCreateWithPhone();

  if (isLoading) return <CenteredRegion><p>{t("common.states.loading")}</p></CenteredRegion>;
  if (!isAuthenticated || !hasAccess) return <CenteredRegion><h1>{t("admin.forbiddenTitle")}</h1></CenteredRegion>;

  return <main className="mx-auto max-w-2xl space-y-5 p-6">
    <h1 className="text-2xl font-semibold">{t("admin.users.createTitle")}</h1>
    <form className="space-y-4" onSubmit={event => {
      event.preventDefault();
      const onSuccess = () => router.push("/users");
      if (mode === "email") email.mutate({ data: { firstName, lastName, email: contact } }, { onSuccess });
      else phone.mutate({ data: { firstName, lastName, phoneNumber: contact } }, { onSuccess });
    }}>
      <label className="block">{t("admin.users.creationMethod")}
        <select className="mt-1 rounded border p-2" value={mode} onChange={event => setMode(event.target.value as "email" | "phone")}>
          <option value="email">{t("admin.users.email")}</option><option value="phone">{t("admin.users.phone")}</option>
        </select>
      </label>
      <label className="block">{mode === "email" ? t("admin.users.email") : t("admin.users.phone")}
        <input className="mt-1 w-full rounded border p-2" required value={contact} onChange={event => setContact(event.target.value)} />
      </label>
      <label className="block">{t("admin.users.firstName")}<input className="mt-1 w-full rounded border p-2" value={firstName} onChange={event => setFirstName(event.target.value)} /></label>
      <label className="block">{t("admin.users.lastName")}<input className="mt-1 w-full rounded border p-2" value={lastName} onChange={event => setLastName(event.target.value)} /></label>
      <button className="rounded bg-primary px-4 py-2 text-primary-foreground" disabled={email.isPending || phone.isPending}>{t("admin.users.create")}</button>
      {(email.isError || phone.isError) && <p className="text-danger">{t("admin.users.createError")}</p>}
    </form>
  </main>;
}
