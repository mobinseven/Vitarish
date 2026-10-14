"use client";

import { useCallback, useState } from "react";
import { Alert, Button, Surface, Tooltip } from "@heroui/react";
import { getLocalTimeZone } from "@internationalized/date";
import {
  ErrorStateSurface,
  FaIcon,
  HeaderIcon,
  LoadingStateSurface,
} from "@vitarish/ui";
import { ApiError } from "../../api/client/apiFetch";
import { localizeApiError, useLocalizedText } from "../../localization/index";
import {
  emptySurgeryPlanForm,
  surgeryPlanFormFromSurgery,
  type SurgeryPlanFormValues,
} from "../../models/surgeries/surgeryPlanForm";
import { canEditSurgery, type Surgery } from "../../models/surgeries/surgeryModels";
import type { NavigationIntent } from "../../navigation";
import { usePatientDetail } from "../../hooks/patients/usePatientDetail";
import { useCanPlanSurgeries } from "../../hooks/surgeries/useCanPlanSurgeries";
import { useSurgery } from "../../hooks/surgeries/useSurgery";
import {
  isSurgeryConflictError,
  useSurgeryMutations,
} from "../../hooks/surgeries/useSurgeryMutations";
import { useClinicStaff } from "../../hooks/surgeries/useClinicStaff";
import { PatientFindOrCreate } from "../patients/find-or-create/PatientFindOrCreate";
import { SurgeryPlanForm } from "./SurgeryPlanForm";

export interface SurgeryPlanPageProps {
  mode: "create" | "edit";
  /** Present when `mode` is "edit". */
  surgeryId?: string;
  /** Preselected patient when creating, from `?patientId=`. */
  initialPatientId?: string;
  onNavigate: (intent: NavigationIntent) => void;
}

function httpStatus(error: unknown): number {
  if (typeof error !== "object" || error === null || !("status" in error)) return 0;
  return Number((error as { status?: number | string }).status);
}

function PatientSummaryHeader() {
  const { t } = useLocalizedText();
  return (
    <div className="flex items-center gap-3">
      <HeaderIcon icon="user" />
      <div className="min-w-0">
        <h2 className="text-lg font-semibold text-foreground">
          {t("clinic.surgeries.plan.patientSectionTitle")}
        </h2>
      </div>
    </div>
  );
}

/** Fallback label when the patient record has no display name. */
function UnnamedPatient() {
  const { t } = useLocalizedText();
  return <>{t("clinic.patients.unnamedPatient")}</>;
}

export function SurgeryPlanPage({
  mode,
  surgeryId,
  initialPatientId,
  onNavigate,
}: SurgeryPlanPageProps) {
  const { t, direction } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const timeZone = getLocalTimeZone();
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  const [patientId, setPatientId] = useState(initialPatientId ?? "");
  const [submitError, setSubmitError] = useState<string | null>(null);

  const surgeryQuery = useSurgery(mode === "edit" ? surgeryId ?? "" : "");
  const staffQuery = useClinicStaff();
  const surgery = surgeryQuery.data;
  const lookupPatientId = mode === "edit" ? surgery?.patientId ?? "" : patientId;
  const patientQuery = usePatientDetail(lookupPatientId);
  const mutations = useSurgeryMutations();

  const handleCancel = useCallback(() => {
    if (mode === "edit" && surgeryId) {
      onNavigate({ kind: "surgeryDetail", surgeryId });
    } else {
      onNavigate({ kind: "surgeryIndex" });
    }
  }, [mode, surgeryId, onNavigate]);

  const handleError = useCallback(
    (error: unknown) => {
      // The server answered, so the next submit is a new request; a network failure keeps the key so a retry stays idempotent.
      if (error instanceof ApiError) setIdempotencyKey(crypto.randomUUID());
      if (isSurgeryConflictError(error)) {
        setSubmitError(t("clinic.surgeries.errors.conflict"));
        if (mode === "edit") void surgeryQuery.refetch();
        return;
      }
      switch (httpStatus(error)) {
        case 503:
          setSubmitError(t("clinic.surgeries.errors.unavailable"));
          return;
        case 403:
          setSubmitError(t("clinic.surgeries.errors.ownerOnly"));
          return;
        case 404:
          setSubmitError(t("clinic.surgeries.errors.notFound"));
          return;
        default:
          setSubmitError(localizeApiError(error, t));
      }
    },
    [mode, surgeryQuery, t],
  );

  const handleSubmit = useCallback(
    (values: SurgeryPlanFormValues) => {
      setSubmitError(null);
      const staff = staffQuery.data ?? [];
      const onSuccess = (result: Surgery) =>
        onNavigate({ kind: "surgeryDetail", surgeryId: result.id });
      if (mode === "create") {
        mutations.create.mutate(
          { values, idempotencyKey, timeZone, staff },
          { onError: handleError, onSuccess },
        );
      } else if (surgery) {
        mutations.update.mutate(
          { surgery, values, timeZone, staff },
          { onError: handleError, onSuccess },
        );
      }
    },
    [
      mode,
      idempotencyKey,
      timeZone,
      staffQuery.data,
      surgery,
      mutations,
      handleError,
      onNavigate,
    ],
  );

  const isSubmitting = mutations.create.isPending || mutations.update.isPending;

  const header = (
    <header className="flex items-center gap-4">
      <HeaderIcon icon="scissors" level="page" />
      <div className="min-w-0">
        <h1 className="text-2xl font-bold text-foreground">
          {mode === "create"
            ? t("clinic.surgeries.plan.createTitle")
            : t("clinic.surgeries.plan.editTitle")}
        </h1>
      </div>
    </header>
  );

  if (!canPlan) {
    return (
      <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 pt-8">
        {header}
        <Alert status="default">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("clinic.surgeries.errors.ownerOnly")}</Alert.Title>
          </Alert.Content>
        </Alert>
      </div>
    );
  }

  const patientName = (() => {
    const patient = patientQuery.data;
    if (!patient) return null;
    const name = [patient.firstName, patient.lastName]
      .filter((part): part is string => Boolean(part && part.trim()))
      .join(" ")
      .trim();
    return name.length > 0 ? name : null;
  })();

  const patientSummary = (
    <Surface className="flex flex-col gap-4 rounded-3xl p-6">
      <PatientSummaryHeader />
      <div className="flex items-center gap-3">
        <span
          aria-hidden="true"
          className="flex size-10 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-foreground"
        >
          <FaIcon aria-hidden="true" icon="user" />
        </span>
        <span className="min-w-0 flex-1 truncate text-sm font-medium text-foreground">
          {patientName ?? (patientQuery.isPending ? null : <UnnamedPatient />)}
        </span>
        {mode === "create" ? (
          <Tooltip>
            <Button variant="tertiary" onPress={() => setPatientId("")}>
              <FaIcon aria-hidden="true" icon="user-pen" />
              {t("clinic.surgeries.plan.changePatient")}
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.plan.changePatient")}
            </Tooltip.Content>
          </Tooltip>
        ) : null}
      </div>
    </Surface>
  );

  if (mode === "edit") {
    if (surgeryQuery.isPending) {
      return (
        <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 pt-8">
          {header}
          <LoadingStateSurface label={t("clinic.surgeries.plan.loading")} />
        </div>
      );
    }

    if (surgeryQuery.isError || !surgery) {
      return (
        <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 pt-8">
          {header}
          <ErrorStateSurface
            description={t("clinic.surgeries.errors.unavailable")}
            title={t("clinic.surgeries.errors.loadFailed")}
          >
            <Tooltip>
              <Button variant="outline" onPress={() => void surgeryQuery.refetch()}>
                <FaIcon aria-hidden="true" icon="rotate-right" />
                {t("clinic.surgeries.actions.retry")}
              </Button>
              <Tooltip.Content>
                {t("clinic.surgeries.actions.retry")}
              </Tooltip.Content>
            </Tooltip>
          </ErrorStateSurface>
        </div>
      );
    }

    if (!canEditSurgery(surgery)) {
      return (
        <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 pt-8">
          {header}
          <Alert status="warning">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Title>
                {t("clinic.surgeries.plan.notEditableTitle")}
              </Alert.Title>
              <Alert.Description>
                {t("clinic.surgeries.plan.notEditableDescription")}
              </Alert.Description>
            </Alert.Content>
          </Alert>
          <div>
            <Tooltip>
              <Button
                variant="tertiary"
                onPress={() =>
                  onNavigate({ kind: "surgeryDetail", surgeryId: surgery.id })
                }
              >
                <FaIcon
                  aria-hidden="true"
                  icon={direction === "rtl" ? "arrow-right" : "arrow-left"}
                />
                {t("clinic.surgeries.plan.backToSurgery")}
              </Button>
              <Tooltip.Content>
                {t("clinic.surgeries.plan.backToSurgery")}
              </Tooltip.Content>
            </Tooltip>
          </div>
        </div>
      );
    }

    return (
      <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 pt-8">
        {header}
        {patientSummary}
        <SurgeryPlanForm
          key={surgery.id}
          excludeSurgeryId={surgery.id}
          initialValues={surgeryPlanFormFromSurgery(surgery, timeZone)}
          isSubmitting={isSubmitting}
          mode="edit"
          staff={staffQuery.data ?? []}
          staffLoading={staffQuery.isPending}
          submitError={submitError}
          timeZone={timeZone}
          onCancel={handleCancel}
          onSubmit={handleSubmit}
        />
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 pt-8">
      {header}
      {patientId ? (
        <>
          {patientSummary}
          <SurgeryPlanForm
            key={patientId}
            initialValues={emptySurgeryPlanForm(patientId)}
            isSubmitting={isSubmitting}
            mode="create"
            staff={staffQuery.data ?? []}
            staffLoading={staffQuery.isPending}
            submitError={submitError}
            timeZone={timeZone}
            onCancel={handleCancel}
            onSubmit={handleSubmit}
          />
        </>
      ) : (
        <Surface className="flex flex-col gap-6 rounded-3xl p-6">
          <div className="flex items-center gap-3">
            <HeaderIcon icon="user-plus" />
            <div className="min-w-0">
              <h2 className="text-lg font-semibold text-foreground">
                {t("clinic.surgeries.plan.patientSectionTitle")}
              </h2>
              <p className="text-sm text-muted">
                {t("clinic.surgeries.plan.patientSectionDescription")}
              </p>
            </div>
          </div>
          <PatientFindOrCreate
            hideSelectedState
            onPatientSelected={setPatientId}
          />
        </Surface>
      )}
    </div>
  );
}
