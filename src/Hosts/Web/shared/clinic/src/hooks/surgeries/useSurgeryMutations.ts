import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  clinicCancelSurgery,
  clinicCompleteSurgery,
  clinicCreatePatientSurgery,
  clinicUpdateSurgery,
} from "../../api/generated/clinic/clinic";
import { ApiError } from "../../api/client/apiFetch";
import {
  buildSurgeryCreateRequest,
  buildSurgeryWriteRequest,
  mapSurgeryResponse,
} from "../../models/surgeries/surgeryMappers";
import type {
  ClinicStaffMember,
  Surgery,
} from "../../models/surgeries/surgeryModels";
import type { SurgeryPlanFormValues } from "../../models/surgeries/surgeryPlanForm";
import { surgeryKeys } from "./surgeryKeys";

/** True when the server rejected a write because the surgery changed since it was read. */
export function isSurgeryConflictError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 409;
}

/** True when the server refused a surgery-planning write because the viewer is not the clinic owner. */
export function isSurgeryForbiddenError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 403;
}

export function useSurgeryMutations() {
  const queryClient = useQueryClient();
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: surgeryKeys.all });

  const create = useMutation({
    mutationFn: ({
      values,
      idempotencyKey,
      timeZone,
      staff,
    }: {
      values: SurgeryPlanFormValues;
      idempotencyKey: string;
      timeZone: string;
      staff: readonly ClinicStaffMember[];
    }) =>
      clinicCreatePatientSurgery(
        values.patientId,
        buildSurgeryCreateRequest(values, { idempotencyKey, timeZone, staff }),
      ).then(mapSurgeryResponse),
    onSuccess: invalidate,
  });

  const update = useMutation({
    mutationFn: ({
      surgery,
      values,
      timeZone,
      staff,
    }: {
      surgery: Surgery;
      values: SurgeryPlanFormValues;
      timeZone: string;
      staff: readonly ClinicStaffMember[];
    }) =>
      clinicUpdateSurgery(
        surgery.id,
        buildSurgeryWriteRequest(surgery, values, { timeZone, staff }),
      ).then(mapSurgeryResponse),
    onSuccess: invalidate,
  });

  const complete = useMutation({
    mutationFn: (surgery: Surgery) =>
      clinicCompleteSurgery(surgery.id, {
        concurrencyToken: surgery.concurrencyToken,
      }).then(mapSurgeryResponse),
    onSuccess: invalidate,
  });

  const cancel = useMutation({
    mutationFn: (surgery: Surgery) =>
      clinicCancelSurgery(surgery.id, {
        concurrencyToken: surgery.concurrencyToken,
      }).then(mapSurgeryResponse),
    onSuccess: invalidate,
  });

  return { create, update, complete, cancel };
}
