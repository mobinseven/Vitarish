import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  clinicCreateSurgeryCatalog,
  clinicDeleteSurgeryCatalog,
  clinicUpdateSurgeryCatalog,
} from "../../api/generated/clinic/clinic";
import { mapSurgeryType } from "../../models/surgeries/surgeryMappers";
import type { SurgeryType } from "../../models/surgeries/surgeryModels";
import { surgeryKeys } from "./surgeryKeys";

export function useSurgeryTypeMutations() {
  const queryClient = useQueryClient();
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: surgeryKeys.all });

  const create = useMutation({
    mutationFn: (type: Omit<SurgeryType, "id" | "concurrencyToken">) =>
      clinicCreateSurgeryCatalog({
        name: type.name,
        tools: [...type.tools],
        defaultTeamRoles: [...type.defaultTeamRoles],
        reminderPolicies: type.reminderPolicies.map((policy) => ({ ...policy })),
      }).then(mapSurgeryType),
    onSuccess: invalidate,
  });

  const update = useMutation({
    mutationFn: (type: SurgeryType) =>
      clinicUpdateSurgeryCatalog(type.id, {
        name: type.name,
        tools: [...type.tools],
        defaultTeamRoles: [...type.defaultTeamRoles],
        reminderPolicies: type.reminderPolicies.map((policy) => ({ ...policy })),
        concurrencyToken: type.concurrencyToken,
      }).then(mapSurgeryType),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: async (type: SurgeryType) => {
      await clinicDeleteSurgeryCatalog(type.id, {
        concurrencyToken: type.concurrencyToken,
      });
    },
    onSuccess: invalidate,
  });

  return { create, update, remove };
}
