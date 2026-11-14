/**
 * Operations prepared-action types the review renders specially, as the clinic API stores and returns them
 * (`appointment_manage` in the Clinic operations capability, `AssistantStaffInvitation.ActionType`,
 * `AssistantStaffManagement.RemoveActionType`, `ClinicSetupAssistant.ProfileActionType`).
 */
export const operationsActionTypes = {
  appointmentManage: "appointment_manage",
  inviteMember: "invite_member",
  removeMember: "remove_member",
  updateClinicProfile: "update_clinic_profile",
} as const;
