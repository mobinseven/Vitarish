"use client";

import { Alert, Avatar, Button, Chip, Spinner, Surface, Tooltip } from "@heroui/react";
import { useLocalizedText, type MessageKey } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import type { SignInClinicDto } from "../../api/generated/parties/model";
import { useClinicSelection } from "./useClinicSelection";

const roleLabelKeys: Record<string, MessageKey> = {
  TenantOwner: "journey.gates.workspace.roles.owner",
  TenantUser: "journey.gates.workspace.roles.staff",
};

function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  return words.slice(0, 2).map((word) => word[0]).join("").toUpperCase();
}

interface ClinicSelectorProps {
  /** The app route on each host that redeems the one-time sign-in token. */
  handoffPath: string;
}

/** Lists the signed-in person's clinics on the shared sign-in host and continues, signed in, to the one they choose. */
export function ClinicSelector({ handoffPath }: ClinicSelectorProps) {
  const { t } = useLocalizedText();
  const selection = useClinicSelection(handoffPath);

  if (selection.isLoading) {
    return <Spinner aria-label={t("journey.loading.workspaceOptions")} className="self-center" />;
  }

  if (selection.loadFailed) {
    return (
      <Alert status="danger">
        <Alert.Indicator />
        <Alert.Content>{t("journey.gates.workspace.loadFailed")}</Alert.Content>
      </Alert>
    );
  }

  const opening = selection.opening;
  const openingClinic = opening?.kind === "clinic"
    ? selection.clinics.find((clinic) => clinic.tenantId === opening.tenantId)
    : undefined;
  // With one clinic or none the page continues by itself, so it only says where it is going.
  if (opening && selection.clinics.length <= 1) {
    return (
      <div className="flex flex-col items-center gap-3 text-center" role="status">
        <Spinner aria-hidden="true" />
        <p className="text-base text-muted">
          {openingClinic
            ? t("journey.gates.workspace.opening", { name: openingClinic.name })
            : t("journey.gates.workspace.openingNewClinic")}
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {selection.openFailed ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>{t("journey.gates.workspace.openFailed")}</Alert.Content>
        </Alert>
      ) : null}
      {selection.clinics.length === 0 ? (
        <Alert status="warning">
          <Alert.Indicator />
          <Alert.Content>{t("journey.gates.workspace.noClinics")}</Alert.Content>
        </Alert>
      ) : (
        <ul className="flex flex-col gap-3" aria-label={t("journey.gates.workspace.title")}>
          {selection.clinics.map((clinic) => (
            <li key={clinic.tenantId}>
              <ClinicRow
                clinic={clinic}
                isOpening={openingClinic?.tenantId === clinic.tenantId}
                isDisabled={selection.opening !== null}
                onOpen={() => selection.open({ kind: "clinic", tenantId: clinic.tenantId })}
              />
            </li>
          ))}
        </ul>
      )}
      {selection.newClinicUnavailable ? null : (
        <Tooltip>
          <Button
            variant="tertiary"
            className="self-center"
            isDisabled={selection.opening !== null}
            isPending={selection.opening?.kind === "newClinic"}
            onPress={() => selection.open({ kind: "newClinic" })}
          >
            <FaIcon icon="plus" aria-hidden="true" />
            {t("journey.gates.workspace.createAction")}
          </Button>
          <Tooltip.Content>{t("journey.gates.workspace.createHint")}</Tooltip.Content>
        </Tooltip>
      )}
    </div>
  );
}

interface ClinicRowProps {
  clinic: SignInClinicDto;
  isOpening: boolean;
  isDisabled: boolean;
  onOpen: () => void;
}

function ClinicRow({ clinic, isOpening, isDisabled, onOpen }: ClinicRowProps) {
  const { t } = useLocalizedText();
  const roleKey = roleLabelKeys[clinic.role];

  return (
    <Surface variant="secondary" className="flex items-center gap-4 rounded-2xl p-4">
      <Avatar className="size-12 shrink-0">
        <Avatar.Fallback className="bg-accent text-accent-foreground">{initials(clinic.name)}</Avatar.Fallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="truncate text-lg font-semibold text-foreground">{clinic.name}</span>
        {/* The address reads left to right but sits at the start of the line in either direction. */}
        <span dir="ltr" className="max-w-full self-start truncate text-base text-muted">{clinic.hostname}</span>
        {roleKey ? (
          <Chip size="md" variant="soft" className="self-start">
            <Chip.Label>{t(roleKey)}</Chip.Label>
          </Chip>
        ) : null}
      </div>
      <Tooltip>
        <Button
          aria-label={t("journey.gates.workspace.openAction", { name: clinic.name })}
          isDisabled={isDisabled}
          isPending={isOpening}
          onPress={onOpen}
        >
          <FaIcon icon="arrow-right-to-bracket" aria-hidden="true" className="rtl:-scale-x-100" />
          <span className="max-sm:hidden">{t("journey.gates.workspace.continueAction")}</span>
        </Button>
        <Tooltip.Content>{t("journey.gates.workspace.openHint")}</Tooltip.Content>
      </Tooltip>
    </Surface>
  );
}
