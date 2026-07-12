"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import type { ClinicNavigationIntent } from "@vitarish/clinic-web";

export function useClinicNavigation() {
  const router = useRouter();

  const navigate = useCallback(
    (intent: ClinicNavigationIntent) => {
      switch (intent.kind) {
        case "patientDetail":
          router.push(Routes.Clinic.Patients.Detail(intent.patientId));
          break;
        case "patientNew":
          router.push(Routes.Clinic.Patients.New);
          break;
        case "patientEdit":
          router.push(Routes.Clinic.Patients.Edit(intent.patientId));
          break;
        case "patientIndex":
          router.push(Routes.Clinic.Patients.Index);
          break;
        case "patientProcedureDetail":
          router.push(Routes.Clinic.Patients.Procedure(intent.patientId, intent.procedureId));
          break;
        case "patientProcedureEventDetail":
          router.push(
            Routes.Clinic.Patients.ProcedureEvent(intent.patientId, intent.procedureId, intent.eventId)
          );
          break;
        case "checkIn":
          router.push(Routes.Clinic.CheckIn);
          break;
        case "settingsIndex":
          router.push(Routes.Clinic.Settings.Index);
          break;
        case "settingsUsers":
          router.push(Routes.Clinic.Settings.Users);
          break;
        case "settingsStatistics":
          router.push(Routes.Clinic.Settings.Statistics);
          break;
        case "reminders":
          router.push(Routes.Clinic.Reminders);
          break;
        case "serviceTypes":
          router.push(Routes.Clinic.Appointments.ServiceTypes.Index);
          break;
        case "services":
          router.push(Routes.Clinic.Appointments.Services.Index);
          break;
        case "serviceProviders":
          router.push(Routes.Clinic.Appointments.ServiceProviders.Index);
          break;
        case "shiftDetail":
          router.push(Routes.Clinic.Appointments.Shifts.Detail(intent.shiftId));
          break;
        case "shiftsNew":
          router.push(Routes.Clinic.Appointments.Shifts.New);
          break;
        case "reservationDetail":
          router.push(Routes.Clinic.Appointments.Reservations.Detail(intent.reservationId));
          break;
        case "userShiftDetail":
          router.push(`/user/clinic/shifts/${intent.shiftId}`);
          break;
        case "userReservationDetail":
          router.push(`/user/clinic/reservations/${intent.reservationId}`);
          break;
      }
    },
    [router]
  );

  return navigate;
}
