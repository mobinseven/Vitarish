"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import type { NavigationIntent } from "@vitarish/clinic-web";

export function useClinicNavigation() {
  const router = useRouter();

  const navigate = useCallback(
    (intent: NavigationIntent) => {
      switch (intent.kind) {
        case "dashboard":
          router.push(Routes.Clinic.Index);
          break;
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
          router.push(
            Routes.Clinic.Patients.Procedure(
              intent.patientId,
              intent.procedureId,
            ),
          );
          break;
        case "patientProcedureEventDetail":
          router.push(
            Routes.Clinic.Patients.ProcedureEvent(
              intent.patientId,
              intent.procedureId,
              intent.eventId,
            ),
          );
          break;
        case "checkIn":
          router.push(Routes.Clinic.CheckIn);
          break;
        case "appointmentSettings":
          router.push(Routes.Clinic.Appointments.Settings);
          break;
        case "clinicSettings":
          router.push(Routes.Clinic.Settings.Index);
          break;
        case "clinicSettingsUsers":
          router.push(Routes.Clinic.Settings.Users);
          break;
        case "clinicSettingsInsurance":
          router.push(Routes.Clinic.Settings.Insurance);
          break;
        case "clinicStatistics":
          router.push(Routes.Clinic.Settings.Statistics);
          break;
        case "procedureTemplateManagement":
          router.push(Routes.Clinic.Settings.ProcedureTemplates);
          break;
        case "prescriptionTemplateManagement":
          router.push(Routes.Clinic.Settings.PrescriptionTemplates);
          break;
        case "reminders":
          router.push(Routes.Clinic.Reminders);
          break;
        case "serviceTypeIndex":
          router.push(Routes.Clinic.Appointments.ServiceTypes.Index);
          break;
        case "serviceTypeDetail":
          router.push(
            Routes.Clinic.Appointments.ServiceTypes.Detail(
              intent.serviceTypeId,
            ),
          );
          break;
        case "serviceIndex":
          router.push(Routes.Clinic.Appointments.Services.Index);
          break;
        case "serviceDetail":
          router.push(
            Routes.Clinic.Appointments.Services.Detail(intent.serviceId),
          );
          break;
        case "serviceProviderIndex":
          router.push(Routes.Clinic.Appointments.ServiceProviders.Index);
          break;
        case "serviceProviderDetail":
          router.push(
            Routes.Clinic.Appointments.ServiceProviders.Detail(
              intent.serviceProviderId,
            ),
          );
          break;
        case "ownerShiftIndex":
          router.push(Routes.Clinic.Appointments.Shifts.Index);
          break;
        case "ownerShiftDetail":
          router.push(Routes.Clinic.Appointments.Shifts.Detail(intent.shiftId));
          break;
        case "ownerShiftCreate":
          router.push(Routes.Clinic.Appointments.Shifts.New);
          break;
        case "ownerShiftEdit":
          router.push(Routes.Clinic.Appointments.Shifts.Edit(intent.shiftId));
          break;
        case "ownerShiftOccurrence":
          router.push(
            Routes.Clinic.Appointments.Shifts.Occurrence(
              intent.shiftId,
              intent.occurrenceDate,
            ),
          );
          break;
        case "ownerReservationIndex":
          router.push(Routes.Clinic.Appointments.Reservations.Index);
          break;
        case "ownerReservationDetail":
          router.push(
            Routes.Clinic.Appointments.Reservations.Detail(
              intent.reservationId,
            ),
          );
          break;
        case "userShiftIndex":
          router.push(Routes.ClinicUser.Shifts.Index);
          break;
        case "userShiftDetail":
          router.push(Routes.ClinicUser.Shifts.Detail(intent.shiftId));
          break;
        case "userReservationIndex":
          router.push(Routes.ClinicUser.Reservations.Index);
          break;
        case "userReservationDetail":
          router.push(
            Routes.ClinicUser.Reservations.Detail(intent.reservationId),
          );
          break;
      }
    },
    [router],
  );

  return navigate;
}
