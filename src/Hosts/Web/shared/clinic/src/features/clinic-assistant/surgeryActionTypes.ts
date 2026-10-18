import type { MessageKey } from "../../localization";

/**
 * The surgery workflow's prepared-action types, as the clinic API stores and returns them
 * (`SurgeryAssistantActions` in the Clinic app). Each maps to the label of what it does;
 * the patient and practice variants of a surgery change read the same.
 */
export const surgeryActionLabelKeys = {
  create_patient_surgery: "clinic.surgeries.actions.plan",
  update_patient_surgery: "clinic.surgeries.actions.edit",
  complete_patient_surgery: "clinic.surgeries.actions.complete",
  cancel_patient_surgery: "clinic.surgeries.actions.cancel",
  create_surgery: "clinic.surgeries.actions.plan",
  update_surgery: "clinic.surgeries.actions.edit",
  complete_surgery: "clinic.surgeries.actions.complete",
  cancel_surgery: "clinic.surgeries.actions.cancel",
  create_surgery_type: "clinic.surgeries.assistant.actions.createType",
  update_surgery_type: "clinic.surgeries.assistant.actions.editType",
  delete_surgery_type: "clinic.surgeries.assistant.actions.deleteType",
  update_surgery_cancellation_message: "clinic.surgeries.assistant.actions.editCancellationMessage",
} as const satisfies Record<string, MessageKey>;

export type SurgeryActionType = keyof typeof surgeryActionLabelKeys;

export function isSurgeryActionType(actionType: string): actionType is SurgeryActionType {
  return Object.hasOwn(surgeryActionLabelKeys, actionType);
}
