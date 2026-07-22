"use client";

import { use } from "react";
import { useAuthSession } from "@vitarish/auth-web";
import { PatientProcedureEventPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

interface PatientProcedureEventRouteProps {
  params: Promise<{ patientId: string; procedureId: string; eventId: string }>;
}

export default function PatientProcedureEventRoute({
  params,
}: PatientProcedureEventRouteProps) {
  const { patientId, procedureId, eventId } = use(params);
  const { displayProfile, user } = useAuthSession();
  const onNavigate = useClinicNavigation();
  const viewer = user
    ? {
        id: user.userId,
        label: displayProfile.displayLabel,
      }
    : undefined;

  return (
    <PatientProcedureEventPage
      eventId={eventId}
      patientId={patientId}
      procedureId={procedureId}
      viewer={viewer}
      onNavigate={onNavigate}
    />
  );
}
