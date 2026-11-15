import type { MessageKey } from "../../localization";

/**
 * Prepared changes to a patient's procedures and steps, as the clinic API stores and returns them
 * (`ProcedureActionTypes` in the Clinic app). The patient-page and practice variants review the same.
 */
export const procedureActionTypes = [
  "procedure_step_change",
  "procedure_create",
  "operations_procedure_step_change",
  "operations_procedure_create",
] as const;

export type ProcedureActionType = (typeof procedureActionTypes)[number];

export function isProcedureActionType(actionType: string): actionType is ProcedureActionType {
  return (procedureActionTypes as readonly string[]).includes(actionType);
}

/** What a procedure change does (`ProcedureChangeKinds` in the Clinic app), mapped to its review title. */
export const procedureChangeTitleKeys = {
  commit_step: "clinic.clinicAssistant.procedureChange.title.commit_step",
  reopen_step: "clinic.clinicAssistant.procedureChange.title.reopen_step",
  add_note: "clinic.clinicAssistant.procedureChange.title.add_note",
  add_reminder: "clinic.clinicAssistant.procedureChange.title.add_reminder",
  add_procedure: "clinic.clinicAssistant.procedureChange.title.add_procedure",
} as const satisfies Record<string, MessageKey>;

export function procedureChangeTitleKey(kind: string | null): MessageKey | null {
  return kind && Object.hasOwn(procedureChangeTitleKeys, kind)
    ? procedureChangeTitleKeys[kind as keyof typeof procedureChangeTitleKeys]
    : null;
}

/** Whether a stored review payload describes a procedure change this client can show; only then may it be confirmed. */
export function isDescribableProcedureChange(reviewPayload: unknown): boolean {
  const change =
    reviewPayload && typeof reviewPayload === "object" ? (reviewPayload as Record<string, unknown>).change : null;
  const kind = change && typeof change === "object" ? (change as Record<string, unknown>).kind : null;
  return procedureChangeTitleKey(typeof kind === "string" ? kind : null) !== null;
}
