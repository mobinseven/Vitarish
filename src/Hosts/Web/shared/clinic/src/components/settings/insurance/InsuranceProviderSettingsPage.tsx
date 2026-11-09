"use client";

import { Alert, Button, Card, Chip, Input, Label, Spinner, TextField, Tooltip } from "@heroui/react";
import { FaIcon, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../../navigation/usePageBack";
import { useEffect, useState } from "react";
import { useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import {
  type TaminCredentialInput,
  useInsuranceProviderSetup,
} from "../../../hooks/settings/insurance/useInsuranceProviderSetup";

const emptyInput: TaminCredentialInput = {
  providerDoctorIdentifier: "",
  providerNationalCode: "",
  providerDoctorMobile: "",
  providerClinicIdentifier: "",
};

export function InsuranceProviderSettingsPage({ onNavigate }: { onNavigate?: (intent: NavigationIntent) => void }) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings], onNavigate);
  const { context, credentials, authorize } = useInsuranceProviderSetup();
  const credential = credentials.data?.[0];
  const [input, setInput] = useState<TaminCredentialInput>(emptyInput);

  useEffect(() => {
    if (!credential) return;
    setInput({
      credentialId: credential.credentialId,
      providerDoctorIdentifier: credential.providerDoctorIdentifier ?? "",
      providerNationalCode: credential.providerUsername ?? "",
      providerDoctorMobile: credential.providerDoctorMobile ?? "",
      providerClinicIdentifier: credential.providerClinicIdentifier ?? "",
    });
  }, [credential]);

  const pending = context.isLoading || credentials.isLoading;
  const invalid =
    !input.providerDoctorIdentifier.trim() ||
    !input.providerNationalCode.trim() ||
    !input.providerDoctorMobile.trim() ||
    !input.providerClinicIdentifier.trim();

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6">
      <PageHeader
        icon="shield-heart"
        title={t("clinic.settings.insurance.title")}
        details={t("clinic.settings.insurance.description")}
        back={back}
      />

      {context.isError || credentials.isError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {t("clinic.settings.insurance.loadFailed")}
            </Alert.Title>
          </Alert.Content>
        </Alert>
      ) : null}

      {authorize.error ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {t("clinic.settings.insurance.authorizationFailed")}
            </Alert.Title>
            <Alert.Description>
              {t("clinic.settings.insurance.tryAgain")}
            </Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}

      <Card>
        <Card.Header className="flex-row items-start justify-between gap-3">
          <div>
            <Card.Title>{t("clinic.settings.insurance.taminTitle")}</Card.Title>
            <Card.Description>
              {t("clinic.settings.insurance.taminDescription")}
            </Card.Description>
          </div>
          {credential ? (
            <Chip
              color={credential.status === 1 ? "success" : "warning"}
              variant="soft"
            >
              {credential.status === 1
                ? t("clinic.settings.insurance.active")
                : t("clinic.settings.insurance.pending")}
            </Chip>
          ) : null}
        </Card.Header>
        <Card.Content className="grid gap-4 md:grid-cols-2">
          {pending ? (
            <div className="col-span-full flex justify-center py-8">
              <Spinner aria-label={t("clinic.settings.insurance.loading")} />
            </div>
          ) : (
            <>
              <TextField
                isRequired
                value={input.providerDoctorIdentifier}
                onChange={(value) =>
                  setInput((current) => ({
                    ...current,
                    providerDoctorIdentifier: value,
                  }))
                }
              >
                <Label>{t("clinic.settings.insurance.doctorIdentifier")}</Label>
                <Input />
              </TextField>
              <TextField
                isRequired
                value={input.providerNationalCode}
                onChange={(value) =>
                  setInput((current) => ({
                    ...current,
                    providerNationalCode: value,
                  }))
                }
              >
                <Label>{t("clinic.settings.insurance.nationalCode")}</Label>
                <Input inputMode="numeric" />
              </TextField>
              <TextField
                isRequired
                value={input.providerDoctorMobile}
                onChange={(value) =>
                  setInput((current) => ({
                    ...current,
                    providerDoctorMobile: value,
                  }))
                }
              >
                <Label>{t("clinic.settings.insurance.doctorMobile")}</Label>
                <Input inputMode="tel" />
              </TextField>
              <TextField
                isRequired
                value={input.providerClinicIdentifier}
                onChange={(value) =>
                  setInput((current) => ({
                    ...current,
                    providerClinicIdentifier: value,
                  }))
                }
              >
                <Label>{t("clinic.settings.insurance.clinicIdentifier")}</Label>
                <Input />
              </TextField>
            </>
          )}
        </Card.Content>
        <Card.Footer className="justify-end">
          <Tooltip>
            <Button
              isIconOnly
              aria-label={
                credential
                  ? t("clinic.settings.insurance.reauthorize")
                  : t("clinic.settings.insurance.authorize")
              }
              isDisabled={pending || invalid}
              isPending={authorize.isPending}
              onPress={() => authorize.mutate(input)}
            >
              {authorize.isPending ? (
                <Spinner color="current" size="sm" />
              ) : (
                <FaIcon icon="arrow-right-to-bracket" aria-hidden="true" />
              )}
            </Button>
            <Tooltip.Content>
              {credential
                ? t("clinic.settings.insurance.reauthorize")
                : t("clinic.settings.insurance.authorize")}
            </Tooltip.Content>
          </Tooltip>
        </Card.Footer>
      </Card>
    </div>
  );
}
