import { ClinicSetupGoal } from "../../api/generated/clinic/model";
import type { NavigationIntent } from "../../navigation";

/** The existing screen each setup goal opens, for people who prefer the menus. */
export const setupGoalIntents: Record<ClinicSetupGoal, NavigationIntent> = {
  [ClinicSetupGoal.ServicesAndProviders]: { kind: "serviceIndex" },
  [ClinicSetupGoal.ShiftsAndBookingRules]: { kind: "ownerShiftIndex" },
  [ClinicSetupGoal.StaffUsers]: { kind: "clinicSettingsUsers" },
  [ClinicSetupGoal.ClinicProfile]: { kind: "clinicSettings" },
};

export function isSetupGoal(value: string): value is ClinicSetupGoal {
  return Object.values(ClinicSetupGoal).includes(value as ClinicSetupGoal);
}
