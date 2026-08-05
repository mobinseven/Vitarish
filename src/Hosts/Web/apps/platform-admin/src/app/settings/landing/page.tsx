"use client";

import { useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { usePlatformAuthorization } from "@vitarish/auth-web";
import {
  getLandingAdministrationGetHostBindingQueryKey,
  getPartiesAdministrationGetTenantIndicatorsQueryKey,
  getPartiesAdministrationGetTenantQueryKey,
  getPartiesAdministrationGetTenantsQueryKey,
  useLandingAdministrationGetHostBinding,
  useLandingAdministrationSetHostBinding,
  usePartiesAdministrationGetTenants,
} from "@vitarish/platform-web";
import { useLocalizedText } from "@vitarish/web-localization";

export default function LandingSettingsPage() {
  const { hasAccess } = usePlatformAuthorization();
  const { t } = useLocalizedText();
  const queryClient = useQueryClient();
  const binding = useLandingAdministrationGetHostBinding({ query: { enabled: hasAccess } });
  const tenants = usePartiesAdministrationGetTenants({ query: { enabled: hasAccess } });
  const mutation = useLandingAdministrationSetHostBinding();
  const [target, setTarget] = useState("");
  const [message, setMessage] = useState("");
  const targetRef = useRef("");

  if (!hasAccess) return null;

  const selectTarget = (value: string) => {
    targetRef.current = value;
    setTarget(value);
  };

  const revalidateAffectedQueries = (previousTenantId: string | null | undefined, tenantId: string) => {
    void queryClient.invalidateQueries({ queryKey: getLandingAdministrationGetHostBindingQueryKey() });
    void queryClient.invalidateQueries({ queryKey: getPartiesAdministrationGetTenantsQueryKey() });
    void queryClient.invalidateQueries({ queryKey: getPartiesAdministrationGetTenantQueryKey(tenantId) });
    void queryClient.invalidateQueries({ queryKey: getPartiesAdministrationGetTenantIndicatorsQueryKey(tenantId) });
    if (previousTenantId && previousTenantId !== tenantId) {
      void queryClient.invalidateQueries({ queryKey: getPartiesAdministrationGetTenantQueryKey(previousTenantId) });
      void queryClient.invalidateQueries({ queryKey: getPartiesAdministrationGetTenantIndicatorsQueryKey(previousTenantId) });
    }
  };

  return <main className="mx-auto max-w-4xl space-y-6 p-6">
    <h1 className="text-2xl font-semibold">{t("admin.landing.title")}</h1>
    <p className="text-muted">{t("admin.landing.description")}</p>
    {binding.isLoading && <p>{t("admin.landing.loading")}</p>}
    {binding.isError && <div><p className="text-danger">{t("admin.landing.error")}</p><button className="underline" onClick={() => binding.refetch()}>{t("admin.landing.retry")}</button></div>}
    {binding.data && <>
      <dl className="space-y-3 rounded border p-4">
        <div><dt className="font-semibold">{t("admin.landing.hostname")}</dt><dd>{binding.data.hostname}</dd></div>
        <div><dt className="font-semibold">{t("admin.landing.tenant")}</dt><dd>{binding.data.tenantId ?? t("admin.landing.unbound")}</dd></div>
      </dl>
      <form className="space-y-3 rounded border p-4" onSubmit={event => {
        event.preventDefault();
        const submittedTarget = targetRef.current;
        if (!submittedTarget || !window.confirm(t("admin.landing.confirmation"))) return;
        setMessage("");
        mutation.mutate({ data: { tenantId: submittedTarget } }, {
          onSuccess: result => {
            if (targetRef.current !== submittedTarget) return;
            revalidateAffectedQueries(result.previousTenantId, result.tenantId);
            setMessage(result.changed ? t("admin.landing.success") : t("admin.landing.noop"));
          },
          onError: () => {
            if (targetRef.current === submittedTarget) setMessage(t("admin.landing.mutationError"));
          },
        });
      }}>
        <label className="block">{t("admin.landing.target")}
          <select className="mt-1 w-full rounded border p-2" value={target} onChange={event => selectTarget(event.target.value)}>
            <option value="">{t("admin.landing.selectTarget")}</option>
            {tenants.data?.map(tenant => <option key={tenant.id} value={tenant.id}>{tenant.name ?? tenant.id} ({tenant.id})</option>)}
          </select>
        </label>
        {tenants.isLoading && <p>{t("admin.tenants.loading")}</p>}
        {tenants.isError && <p className="text-danger">{t("admin.tenants.error")}</p>}
        <p className="text-warning">{t("admin.landing.warning")}</p>
        <button className="rounded bg-primary px-4 py-2 text-primary-foreground" disabled={mutation.isPending || !target}>{t("admin.landing.confirm")}</button>
        {message && <p role="status">{message}</p>}
      </form>
    </>}
  </main>;
}
