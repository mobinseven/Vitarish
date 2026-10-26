"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Alert, Surface } from "@heroui/react";
import { useAuthenticationAuthenticationPolicyGet, useAuthenticationAuthenticationPolicyPut, useAuthenticationSessionPolicyGet, useAuthenticationSessionPolicyPut } from "@vitarish/auth";
import { usePlatformAuthorization } from "@vitarish/auth";
import type { ChangeSecurityAuthenticationPolicyRequest, ChangeSecuritySessionPolicyRequest } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";

function statusOf(error: unknown): number | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const record = error as Record<string, unknown>;
  if (typeof record.status === "number") return record.status;
  if (typeof record.problem === "object" && record.problem !== null && typeof (record.problem as Record<string, unknown>).status === "number") {
    return (record.problem as Record<string, number>).status;
  }
  return undefined;
}

export default function SecuritySettingsPage() {
  const { hasAccess, isLoading: accessLoading } = usePlatformAuthorization();
  const { t } = useLocalizedText();
  const policy = useAuthenticationSessionPolicyGet({ query: { enabled: hasAccess } });
  const save = useAuthenticationSessionPolicyPut();
  const authenticationPolicy = useAuthenticationAuthenticationPolicyGet({ query: { enabled: hasAccess } });
  const saveAuthenticationPolicy = useAuthenticationAuthenticationPolicyPut();
  const [draft, setDraft] = useState<ChangeSecuritySessionPolicyRequest>();
  const [authenticationDraft, setAuthenticationDraft] = useState<ChangeSecurityAuthenticationPolicyRequest>();
  const [sessionMessage, setSessionMessage] = useState("");
  const [authenticationMessage, setAuthenticationMessage] = useState("");

  useEffect(() => {
    if (!policy.data) return;
    setDraft({
      maximumConcurrentSessions: policy.data.maximumConcurrentSessions,
      concurrentSessionsFunctionallyRequired: policy.data.concurrentSessionsFunctionallyRequired,
      defaultInactivityTimeoutMinutes: policy.data.defaultInactivityTimeoutMinutes,
      allowedSessionStartCidrs: policy.data.allowedSessionStartCidrs ?? [],
      expectedVersion: policy.data.version,
    });
  }, [policy.data]);

  useEffect(() => {
    if (!authenticationPolicy.data) return;
    setAuthenticationDraft({
      minimumPasswordLength: authenticationPolicy.data.minimumPasswordLength,
      requirePasswordLowercase: authenticationPolicy.data.requirePasswordLowercase,
      requirePasswordUppercase: authenticationPolicy.data.requirePasswordUppercase,
      requirePasswordNumber: authenticationPolicy.data.requirePasswordNumber,
      requirePasswordSpecialCharacter: authenticationPolicy.data.requirePasswordSpecialCharacter,
      failedAttemptThreshold: authenticationPolicy.data.failedAttemptThreshold,
      sourceFailedAttemptThreshold: authenticationPolicy.data.sourceFailedAttemptThreshold,
      failedAttemptDenialDurationSeconds: authenticationPolicy.data.failedAttemptDenialDurationSeconds,
      failedAttemptResponse: authenticationPolicy.data.failedAttemptResponse,
      enabledAuthenticationMechanisms: authenticationPolicy.data.enabledAuthenticationMechanisms,
      permittedPreAuthenticationActions: authenticationPolicy.data.permittedPreAuthenticationActions,
      expectedVersion: authenticationPolicy.data.version,
    });
  }, [authenticationPolicy.data]);

  if (accessLoading || policy.isLoading || authenticationPolicy.isLoading) return <main className="mx-auto max-w-3xl space-y-4 p-6"><p aria-live="polite">{t("admin.securityPolicy.loading")}</p></main>;
  if (!hasAccess) return <main className="mx-auto max-w-3xl p-6"><p role="alert">{t("admin.securityPolicy.forbidden")}</p></main>;
  if (!draft || !authenticationDraft || policy.isError || authenticationPolicy.isError) return <main className="mx-auto max-w-3xl space-y-4 p-6"><h1 className="text-2xl font-semibold">{t("admin.securityPolicy.title")}</h1><p role="alert">{t("admin.securityPolicy.loadError")}</p><button className="inline-flex items-center gap-2 underline" onClick={() => { void policy.refetch(); void authenticationPolicy.refetch(); }} title={t("admin.securityPolicy.reload")}><i aria-hidden="true" className="fa-light fa-rotate-right" />{t("admin.securityPolicy.reload")}</button></main>;

  const submitAuthentication = async (event: FormEvent) => {
    event.preventDefault();
    setAuthenticationMessage("");
    try {
      const saved = await saveAuthenticationPolicy.mutateAsync({ data: authenticationDraft });
      setAuthenticationDraft((current) => current ? { ...current, expectedVersion: saved.version } : current);
      setDraft((current) => current ? { ...current, expectedVersion: saved.version } : current);
      setAuthenticationMessage(t("admin.securityPolicy.saved"));
      await authenticationPolicy.refetch();
    } catch (error) {
      if (statusOf(error) === 409) { setAuthenticationMessage(t("admin.securityPolicy.conflict")); return; }
      if (statusOf(error) === 403) { setAuthenticationMessage(t("admin.securityPolicy.forbidden")); return; }
      if (statusOf(error) === 503) { setAuthenticationMessage(t("admin.securityPolicy.unavailable")); return; }
      setAuthenticationMessage(t("admin.securityPolicy.saveError"));
    }
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSessionMessage("");
    try {
      const saved = await save.mutateAsync({ data: { ...draft, allowedSessionStartCidrs: draft.allowedSessionStartCidrs } });
      setDraft((current) => current ? { ...current, expectedVersion: saved.version } : current);
      setAuthenticationDraft((current) => current ? { ...current, expectedVersion: saved.version } : current);
      setSessionMessage(t("admin.securityPolicy.saved"));
      await policy.refetch();
    } catch (error) {
      if (statusOf(error) === 409) {
        setSessionMessage(t("admin.securityPolicy.conflict"));
        return;
      }
      if (statusOf(error) === 403) {
        setSessionMessage(t("admin.securityPolicy.forbidden"));
        return;
      }
      if (statusOf(error) === 503) {
        setSessionMessage(t("admin.securityPolicy.unavailable"));
        return;
      }
      setSessionMessage(t("admin.securityPolicy.saveError"));
    }
  };

  return <main className="mx-auto max-w-3xl space-y-6 p-6">
    <header><h1 className="text-2xl font-semibold">{t("admin.securityPolicy.title")}</h1><p className="mt-2 text-muted">{t("admin.securityPolicy.description")}</p></header>
    <Surface className="rounded-3xl p-6">
      <section className="flex flex-col gap-6">
        <div className="flex flex-col items-start gap-1">
          <h2 className="text-lg font-semibold text-foreground">{t("admin.securityPolicy.authenticationTitle")}</h2>
        </div>
        <div className="space-y-4">
          <Alert status="warning">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Title>{t("admin.securityPolicy.authenticationImpact", { minimum: authenticationPolicy.data!.mandatoryPasswordMinimumLength })}</Alert.Title>
            </Alert.Content>
          </Alert>
          <form className="space-y-4" onSubmit={submitAuthentication}>
            <label className="block">{t("admin.securityPolicy.minimumPasswordLength")}<input className="mt-1 w-full rounded border p-2" type="number" min={authenticationPolicy.data!.mandatoryPasswordMinimumLength} required value={authenticationDraft.minimumPasswordLength} onChange={(event) => setAuthenticationDraft({ ...authenticationDraft, minimumPasswordLength: Number(event.target.value) })} /></label>
            <div className="space-y-2"><p className="font-medium" id="password-categories">{t("admin.securityPolicy.passwordCategories")}</p>
              <ul aria-labelledby="password-categories" className="space-y-1">
                {([["requirePasswordLowercase", "lowercase"], ["requirePasswordUppercase", "uppercase"], ["requirePasswordNumber", "number"], ["requirePasswordSpecialCharacter", "special"]] as const).filter(([field]) => authenticationPolicy.data![field]).map(([field, label]) => <li className="flex items-center gap-2" key={field}><i aria-hidden="true" className="fa-light fa-check" />{t(`admin.securityPolicy.password.${label}`)}</li>)}
              </ul>
            </div>
            <label className="block">{t("admin.securityPolicy.failedThreshold")}<input className="mt-1 w-full rounded border p-2" type="number" min={1} required value={authenticationDraft.failedAttemptThreshold} onChange={(event) => setAuthenticationDraft({ ...authenticationDraft, failedAttemptThreshold: Number(event.target.value) })} /></label>
            <label className="block">{t("admin.securityPolicy.sourceFailedThreshold")}<input className="mt-1 w-full rounded border p-2" type="number" min={1} required value={authenticationDraft.sourceFailedAttemptThreshold ?? ""} onChange={(event) => setAuthenticationDraft({ ...authenticationDraft, sourceFailedAttemptThreshold: Number(event.target.value) })} /></label>
            <label className="block">{t("admin.securityPolicy.denialDuration")}<input className="mt-1 w-full rounded border p-2" type="number" min={1} required value={authenticationDraft.failedAttemptDenialDurationSeconds} onChange={(event) => setAuthenticationDraft({ ...authenticationDraft, failedAttemptDenialDurationSeconds: Number(event.target.value) })} /></label>
            <label className="block">{t("admin.securityPolicy.failureResponse")}<select className="mt-1 w-full rounded border p-2" required value={authenticationDraft.failedAttemptResponse} onChange={(event) => setAuthenticationDraft({ ...authenticationDraft, failedAttemptResponse: event.target.value })}>{authenticationPolicy.data!.availableFailedAttemptResponses.map((response) => <option key={response} value={response}>{t(`admin.securityPolicy.response.${response}`)}</option>)}</select></label>
            <fieldset className="space-y-2"><legend className="font-medium">{t("admin.securityPolicy.mechanisms")}</legend>
              {authenticationPolicy.data!.availableAuthenticationMechanisms.map((mechanism) => { const required = authenticationPolicy.data!.requiredAuthenticationMechanisms.includes(mechanism); return <label className="flex items-center gap-2" key={mechanism}><input type="checkbox" checked={authenticationDraft.enabledAuthenticationMechanisms.includes(mechanism)} disabled={required} onChange={(event) => setAuthenticationDraft({ ...authenticationDraft, enabledAuthenticationMechanisms: event.target.checked ? [...authenticationDraft.enabledAuthenticationMechanisms, mechanism] : authenticationDraft.enabledAuthenticationMechanisms.filter((item) => item !== mechanism) })} />{t(`admin.securityPolicy.mechanism.${mechanism}`)}{required && <span className="text-muted">({t("admin.securityPolicy.required")})</span>}</label>; })}
            </fieldset>
            <fieldset className="space-y-2"><legend className="font-medium">{t("admin.securityPolicy.preAuthenticationActions")}</legend>
              {authenticationPolicy.data!.availablePreAuthenticationActions.map((action) => { const required = authenticationPolicy.data!.requiredPreAuthenticationActions.includes(action); return <label className="flex items-center gap-2" key={action}><input type="checkbox" checked={authenticationDraft.permittedPreAuthenticationActions.includes(action)} disabled={required} onChange={(event) => setAuthenticationDraft({ ...authenticationDraft, permittedPreAuthenticationActions: event.target.checked ? [...authenticationDraft.permittedPreAuthenticationActions, action] : authenticationDraft.permittedPreAuthenticationActions.filter((item) => item !== action) })} />{t(`admin.securityPolicy.preAuthenticationAction.${action}`)}{required && <span className="text-muted">({t("admin.securityPolicy.required")})</span>}</label>; })}
            </fieldset>
            <button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={saveAuthenticationPolicy.isPending} title={t("admin.securityPolicy.saveAuthentication")}><i aria-hidden="true" className="fa-light fa-floppy-disk" />{saveAuthenticationPolicy.isPending ? t("admin.securityPolicy.saving") : t("admin.securityPolicy.saveAuthentication")}</button>
          </form>
          {authenticationMessage && <p role="status" aria-live="polite">{authenticationMessage}</p>}
          {saveAuthenticationPolicy.isError && statusOf(saveAuthenticationPolicy.error) === 409 && <button className="inline-flex items-center gap-2 underline" onClick={() => authenticationPolicy.refetch()} title={t("admin.securityPolicy.reload")}><i aria-hidden="true" className="fa-light fa-rotate-right" />{t("admin.securityPolicy.reload")}</button>}
        </div>
      </section>
    </Surface>
    <Surface className="rounded-3xl p-6">
      <section className="flex flex-col gap-6">
        <div className="flex flex-col items-start gap-1">
          <h2 className="text-lg font-semibold text-foreground">{t("admin.securityPolicy.sessionTitle")}</h2>
        </div>
        <div className="space-y-4">
          <Alert status="warning">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Title>{t("admin.securityPolicy.sessionImpact")}</Alert.Title>
            </Alert.Content>
          </Alert>
          <form className="space-y-4" onSubmit={submit}>
            <label className="block">{t("admin.securityPolicy.maximumSessions")}<input className="mt-1 w-full rounded border p-2" type="number" required value={draft.maximumConcurrentSessions} onChange={(event) => setDraft({ ...draft, maximumConcurrentSessions: Number(event.target.value) })} /></label>
            <label className="flex items-center gap-2"><input type="checkbox" checked={draft.concurrentSessionsFunctionallyRequired} onChange={(event) => setDraft({ ...draft, concurrentSessionsFunctionallyRequired: event.target.checked })} />{t("admin.securityPolicy.concurrentSessionsRequired")}</label>
            <label className="block">{t("admin.securityPolicy.defaultIdleMinutes")}<input className="mt-1 w-full rounded border p-2" type="number" required value={draft.defaultInactivityTimeoutMinutes} onChange={(event) => setDraft({ ...draft, defaultInactivityTimeoutMinutes: Number(event.target.value) })} /></label>
            <label className="block">{t("admin.securityPolicy.allowedCidrs")}<textarea className="mt-1 min-h-28 w-full rounded border p-2 font-mono" value={(draft.allowedSessionStartCidrs ?? []).join("\n")} onChange={(event) => setDraft({ ...draft, allowedSessionStartCidrs: event.target.value.split(/\r?\n/).map((cidr) => cidr.trim()).filter(Boolean) })} /></label>
            <button className="inline-flex items-center gap-2 rounded bg-primary px-4 py-2 text-primary-foreground" disabled={save.isPending} title={t("admin.securityPolicy.save")}><i aria-hidden="true" className="fa-light fa-floppy-disk" />{save.isPending ? t("admin.securityPolicy.saving") : t("admin.securityPolicy.save")}</button>
          </form>
          {sessionMessage && <p role="status" aria-live="polite">{sessionMessage}</p>}
          {save.isError && statusOf(save.error) === 409 && <button className="inline-flex items-center gap-2 underline" onClick={() => policy.refetch()} title={t("admin.securityPolicy.reload")}><i aria-hidden="true" className="fa-light fa-rotate-right" />{t("admin.securityPolicy.reload")}</button>}
        </div>
      </section>
    </Surface>
  </main>;
}
