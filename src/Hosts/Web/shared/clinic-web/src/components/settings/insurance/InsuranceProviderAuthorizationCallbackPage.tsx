"use client";

import { Alert, Button, Spinner, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { useEffect } from "react";
import { useLocalizedText } from "../../../localization";
import { useCompleteInsuranceProviderAuthorization } from "../../../hooks/settings/insurance/useInsuranceProviderSetup";

export interface InsuranceProviderAuthorizationCallbackPageProps {
  onReturn: () => void;
}

export function InsuranceProviderAuthorizationCallbackPage({
  onReturn,
}: InsuranceProviderAuthorizationCallbackPageProps) {
  const { t } = useLocalizedText();
  const complete = useCompleteInsuranceProviderAuthorization();

  useEffect(() => {
    complete.mutate();
  }, [complete.mutate]);

  return (
    <div className="mx-auto flex w-full max-w-xl flex-col gap-4 py-12">
      {complete.isPending || complete.isIdle ? (
        <Alert status="accent">
          <Alert.Indicator>
            <Spinner size="sm" />
          </Alert.Indicator>
          <Alert.Content>
            <Alert.Title>{t("clinic.settings.insurance.completing")}</Alert.Title>
            <Alert.Description>
              {t("clinic.settings.insurance.completingDescription")}
            </Alert.Description>
          </Alert.Content>
        </Alert>
      ) : (
        <Alert status={complete.isSuccess ? "success" : "danger"}>
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {complete.isSuccess
                ? t("clinic.settings.insurance.completed")
                : t("clinic.settings.insurance.authorizationFailed")}
            </Alert.Title>
            {complete.error ? (
              <Alert.Description>
                {t("clinic.settings.insurance.tryAgain")}
              </Alert.Description>
            ) : null}
          </Alert.Content>
        </Alert>
      )}
      {!complete.isPending ? (
        <Tooltip>
          <Button
            isIconOnly
            aria-label={t("clinic.settings.insurance.returnToSettings")}
            onPress={onReturn}
          >
            <FaIcon icon="arrow-left" aria-hidden="true" />
          </Button>
          <Tooltip.Content>{t("clinic.settings.insurance.returnToSettings")}</Tooltip.Content>
        </Tooltip>
      ) : null}
    </div>
  );
}
