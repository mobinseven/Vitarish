"use client";

import { Button, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { handoffUrl, hostUrl } from "../handoff/hostUrl";
import type { CreatedTenant } from "./useTenantSetup";

interface TenantReadyPanelProps {
  tenant: CreatedTenant;
  /** The new clinic's page that redeems the one-time sign-in token from the URL fragment. */
  handoffPath: string;
  /** Where the creator signs in on the new clinic when no token could be issued. */
  signInPath: string;
}

export function TenantReadyPanel({ tenant, handoffPath, signInPath }: TenantReadyPanelProps) {
  const { t } = useLocalizedText();

  const goToClinic = () => {
    window.location.assign(tenant.handoffToken
      ? handoffUrl(tenant.hostname, handoffPath, tenant.handoffToken)
      : hostUrl(tenant.hostname, signInPath).toString());
  };

  return (
    <div className="flex flex-col items-center gap-5 text-center">
      <FaIcon icon="circle-check" size="3x" aria-hidden="true" className="text-success" />
      <p className="text-base text-muted">{t("journey.setup.ready.description")}</p>
      <p dir="ltr" className="rounded-2xl bg-default px-4 py-2 font-mono text-lg text-foreground">
        {tenant.hostname}
      </p>
      {tenant.handoffToken ? null : <p className="text-base text-muted">{t("journey.setup.ready.signInThere")}</p>}
      <Tooltip>
        <Button size="lg" onPress={goToClinic}>
          <FaIcon icon="arrow-right-to-bracket" aria-hidden="true" className="rtl:-scale-x-100" />
          {t("journey.setup.ready.goToClinic")}
        </Button>
        <Tooltip.Content>{t("journey.setup.ready.goToClinicHint")}</Tooltip.Content>
      </Tooltip>
    </div>
  );
}
