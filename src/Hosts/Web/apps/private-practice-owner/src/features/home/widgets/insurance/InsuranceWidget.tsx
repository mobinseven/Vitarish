"use client";

import { Button, Card, Surface, Tooltip } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useInsuranceProviderCredentials } from "@vitarish/clinic";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";
import styles from "../HomeWidgets.module.css";

export function InsuranceWidget() {
  const { t } = useLocalizedText();
  const router = useRouter();
  const { context, credentials } = useInsuranceProviderCredentials();
  const rows = credentials.data ?? [];
  return (
    <Card className="relative flex h-full min-h-[620px] flex-col overflow-hidden bg-anchor p-0">
      <div className={`${styles.content} relative z-10 flex h-full flex-col`}>
        <header className="min-h-40 px-5 py-6 text-anchor-foreground sm:min-h-44 sm:px-6">
          <i
            aria-hidden="true"
            className="fa-light fa-shield-check text-2xl text-foreground"
          />
          <h2 className="mt-8 text-2xl font-semibold">
            {t("home.dashboard.insurance")}
          </h2>
          <p className="mt-1 text-sm text-anchor-foreground/75">
            {t("home.dashboard.insuranceDescription")}
          </p>
        </header>
        <Surface className={`${styles.surface} relative z-10 -mt-4 mx-1 flex flex-1 flex-col rounded-3xl p-4 backdrop-blur-xl sm:p-5`}>
            {context.isLoading || credentials.isLoading ? (
              <p className="py-6 text-sm text-muted" aria-live="polite">
                {t("home.dashboard.loadingProviders")}
              </p>
            ) : context.error || credentials.error ? (
              <div
                className="flex items-center justify-between gap-3 py-6"
                role="alert"
              >
                <span className="text-sm text-danger">
                  {t("home.dashboard.providersLoadFailed")}
                </span>
                <Tooltip>
                  <Button
                    size="sm"
                    variant="outline"
                    onPress={() => {
                      if (context.error) void context.refetch();
                      else void credentials.refetch();
                    }}
                  >
                    <i aria-hidden="true" className="fa-light fa-rotate-right" />
                    {t("home.dashboard.retry")}
                  </Button>
                  <Tooltip.Content>{t("home.dashboard.retry")}</Tooltip.Content>
                </Tooltip>
              </div>
            ) : rows.length === 0 ? (
              <p className="py-6 text-sm text-muted">
                {t("home.dashboard.noProviders")}
              </p>
            ) : (
              <div className="divide-y divide-separator">
                {rows.map((row) => {
                  const label =
                    row.providerKey?.trim() || t("home.dashboard.provider");
                  const active = row.status === 1;
                  const disabled =
                    row.status === 3 || row.status === 4 || row.status === 5;
                  const action = active
                    ? t("home.dashboard.addPrescription")
                    : disabled
                      ? t("home.dashboard.providerUnavailable")
                      : row.status === 0
                        ? t("home.dashboard.setupProvider")
                        : t("home.dashboard.reconnectProvider");
                  return (
                    <div
                      className="flex items-center gap-3 py-4"
                      key={row.credentialId ?? label}
                    >
                      <span className={`grid size-9 shrink-0 place-items-center rounded-xl bg-default text-xs font-bold text-foreground`}>
                        {label.slice(0, 2).toUpperCase()}
                      </span>
                      <span className="min-w-0 flex-1 truncate text-sm font-semibold">
                        {label}
                      </span>
                      <Tooltip>
                        <Button
                          isDisabled={disabled}
                          size="sm"
                          variant="outline"
                          onPress={() =>
                            router.push(
                              active
                                ? Routes.Clinic.Settings.PrescriptionTemplates
                                : Routes.Clinic.Settings.Insurance,
                            )
                          }
                        >
                          <i
                            aria-hidden="true"
                            className={
                              active
                                ? "fa-light fa-file-prescription"
                                : "fa-light fa-rotate-right"
                            }
                          />
                          {action}
                        </Button>
                        <Tooltip.Content>
                          {disabled
                            ? t("home.dashboard.providerUnavailableExplanation")
                            : action}
                        </Tooltip.Content>
                      </Tooltip>
                    </div>
                  );
                })}
              </div>
            )}
          <footer className="mt-auto border-t border-separator pt-4">
            <Tooltip>
              <Button
                className="w-full"
                variant="tertiary"
                onPress={() =>
                  router.push(Routes.Clinic.Settings.PrescriptionTemplates)
                }
              >
                <i aria-hidden="true" className="fa-light fa-grid-2" />
                {t("home.dashboard.prescriptionsGrid")}
              </Button>
              <Tooltip.Content>
                {t("home.dashboard.prescriptionsGrid")}
              </Tooltip.Content>
            </Tooltip>
          </footer>
        </Surface>
      </div>
    </Card>
  );
}
