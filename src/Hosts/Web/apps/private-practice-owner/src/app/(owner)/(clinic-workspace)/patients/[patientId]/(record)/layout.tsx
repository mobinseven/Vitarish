"use client";

import { use, useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams, useSelectedLayoutSegment } from "next/navigation";
import {
  PatientRecordShell,
  patientQuickActions,
  toPatientRecordTab,
  type PatientQuickAction,
} from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { PatientDetailSearchParams, Routes } from "@/app/routes";

function toQuickAction(value: string | null): PatientQuickAction | undefined {
  return patientQuickActions.find((action) => action === value);
}

/** The patient's identity and tab strip, shared by every tab of the record. */
export default function PatientRecordLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  const activeTab = toPatientRecordTab(useSelectedLayoutSegment());
  const initialAction = toQuickAction(useSearchParams().get(PatientDetailSearchParams.QuickAction));
  const router = useRouter();
  const forgetInitialAction = useCallback(
    () => router.replace(Routes.Clinic.Patients.Detail(patientId)),
    [router, patientId],
  );

  return (
    <PatientRecordShell
      patientId={patientId}
      activeTab={activeTab}
      onNavigate={useClinicNavigation()}
      initialAction={initialAction}
      onInitialActionHandled={forgetInitialAction}
    >
      {children}
    </PatientRecordShell>
  );
}
