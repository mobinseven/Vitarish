"use client";
import { use, useCallback } from "react";
import { useRouter } from "next/navigation";
import {
  PatientOverviewPage,
  patientQuickActions,
  type PatientQuickAction,
} from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { PatientDetailSearchParams, Routes } from "@/app/routes";

function toQuickAction(value: string | string[] | undefined): PatientQuickAction | undefined {
  return patientQuickActions.find((action) => action === value);
}

export default function PatientOverviewRoute({
  params,
  searchParams,
}: {
  params: Promise<{ patientId: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const { patientId } = use(params);
  const initialAction = toQuickAction(
    use(searchParams)[PatientDetailSearchParams.QuickAction],
  );
  const router = useRouter();
  const forgetInitialAction = useCallback(
    () => router.replace(Routes.Clinic.Patients.Detail(patientId)),
    [router, patientId],
  );
  return (
    <PatientOverviewPage
      patientId={patientId}
      onNavigate={useClinicNavigation()}
      initialAction={initialAction}
      onInitialActionHandled={forgetInitialAction}
    />
  );
}
