"use client";

import { useState, type FormEvent } from "react";
import { Surface } from "@heroui/react";
import { useAuthSession } from "@vitarish/auth";
import { useAccountEmailChange, useAccountPasswordChange, useAccountPhoneChange } from "@vitarish/account";
import { usePlatformAuthorization } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";

export default function MyAccountPage() {
  const { hasAccess, isLoading: accessLoading } = usePlatformAuthorization();
  const { isAuthenticated, user } = useAuthSession();
  const { t } = useLocalizedText();
  const userId = user?.userId ?? "";
  const password = useAccountPasswordChange();
  const email = useAccountEmailChange(userId);
  const phone = useAccountPhoneChange(userId);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [notice, setNotice] = useState("");

  if (accessLoading) return <main className="mx-auto max-w-3xl p-6"><p aria-live="polite">{t("admin.myAccount.loading")}</p></main>;
  if (!hasAccess || !isAuthenticated || !userId) return <main className="mx-auto max-w-3xl p-6"><p role="alert">{t("admin.myAccount.forbidden")}</p></main>;

  const submitPassword = async (event: FormEvent) => {
    event.preventDefault(); setNotice("");
    try { await password.changePassword(currentPassword, newPassword); setCurrentPassword(""); setNewPassword(""); setNotice(t("admin.myAccount.passwordSaved")); }
    catch { setNotice(t("admin.myAccount.changeFailed")); }
  };
  const submitEmail = async (event: FormEvent) => {
    event.preventDefault(); setNotice("");
    try {
      if (email.step === "email") await email.beginEmailChange(email.email);
      else { await email.verifyEmailChange(email.code); setNotice(t("admin.myAccount.emailSaved")); }
    } catch { setNotice(t("admin.myAccount.changeFailed")); }
  };
  const submitPhone = async (event: FormEvent) => {
    event.preventDefault(); setNotice("");
    try {
      if (phone.step === "phone") await phone.beginPhoneChange(phone.phoneNumber);
      else { await phone.verifyPhoneChange(phone.code); setNotice(t("admin.myAccount.phoneSaved")); }
    } catch { setNotice(t("admin.myAccount.changeFailed")); }
  };

  return <main className="mx-auto max-w-3xl space-y-6 p-6">
    <header><h1 className="text-2xl font-semibold">{t("admin.myAccount.title")}</h1><p className="mt-2 text-muted">{t("admin.myAccount.description")}</p></header>
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-1">
        <h2 className="text-lg font-semibold text-foreground">{t("admin.myAccount.passwordTitle")}</h2>
      </div>
      <div className="space-y-3">
        <form className="space-y-3" onSubmit={submitPassword}>
          <label className="block">{t("admin.myAccount.currentPassword")}<input className="mt-1 w-full rounded border p-2" required type="password" autoComplete="current-password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} /></label>
          <label className="block">{t("admin.myAccount.newPassword")}<input className="mt-1 w-full rounded border p-2" required type="password" autoComplete="new-password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} /></label>
          <button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={password.isLoading} title={t("admin.myAccount.savePassword")}><i aria-hidden="true" className="fa-light fa-key" />{t("admin.myAccount.savePassword")}</button>
        </form>
      </div>
    </Surface>
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-1">
        <h2 className="text-lg font-semibold text-foreground">{t("admin.myAccount.emailTitle")}</h2>
      </div>
      <div className="space-y-3">
        {email.profile?.email && <p className="text-sm text-muted">{t("admin.myAccount.currentContact", { value: email.profile.email })}</p>}
        <form className="space-y-3" onSubmit={submitEmail}>
          {email.step === "email" ? <label className="block">{t("admin.myAccount.newEmail")}<input className="mt-1 w-full rounded border p-2" required type="email" autoComplete="email" value={email.email} onChange={(event) => email.setEmail(event.target.value)} /></label> : <><p>{t("admin.myAccount.codeSent")}</p><label className="block">{t("admin.myAccount.verificationCode")}<input className="mt-1 w-full rounded border p-2" required inputMode="numeric" autoComplete="one-time-code" value={email.code} onChange={(event) => email.setCode(event.target.value)} /></label></>}
          <button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={email.isLoading} title={email.step === "email" ? t("admin.myAccount.beginEmail") : t("admin.myAccount.verifyEmail")}><i aria-hidden="true" className="fa-light fa-envelope" />{email.step === "email" ? t("admin.myAccount.beginEmail") : t("admin.myAccount.verifyEmail")}</button>
        </form>
      </div>
    </Surface>
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-1">
        <h2 className="text-lg font-semibold text-foreground">{t("admin.myAccount.phoneTitle")}</h2>
      </div>
      <div className="space-y-3">
        {phone.profile?.phoneNumber && <p className="text-sm text-muted">{t("admin.myAccount.currentContact", { value: phone.profile.phoneNumber })}</p>}
        <form className="space-y-3" onSubmit={submitPhone}>
          {phone.step === "phone" ? <label className="block">{t("admin.myAccount.newPhone")}<input className="mt-1 w-full rounded border p-2" required type="tel" autoComplete="tel" value={phone.phoneNumber} onChange={(event) => phone.setPhoneNumber(event.target.value)} /></label> : <><p>{t("admin.myAccount.codeSent")}</p><label className="block">{t("admin.myAccount.verificationCode")}<input className="mt-1 w-full rounded border p-2" required inputMode="numeric" autoComplete="one-time-code" value={phone.code} onChange={(event) => phone.setCode(event.target.value)} /></label></>}
          <button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={phone.isLoading} title={phone.step === "phone" ? t("admin.myAccount.beginPhone") : t("admin.myAccount.verifyPhone")}><i aria-hidden="true" className="fa-light fa-phone" />{phone.step === "phone" ? t("admin.myAccount.beginPhone") : t("admin.myAccount.verifyPhone")}</button>
        </form>
      </div>
    </Surface>
    {notice && <p role="status" aria-live="polite">{notice}</p>}
  </main>;
}
