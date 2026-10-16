import { useMutation, useQueryClient } from "@tanstack/react-query";
import { clinicUpdateSurgerySettings } from "../../api/generated/clinic/clinic";
import { mapSurgerySettings } from "../../models/surgeries/surgeryMappers";
import type { SurgerySettings } from "../../models/surgeries/surgeryModels";
import { surgeryKeys } from "./surgeryKeys";

export function useSurgerySettingsMutation() {
  const queryClient = useQueryClient();

  const update = useMutation({
    mutationFn: (settings: SurgerySettings) =>
      clinicUpdateSurgerySettings({
        cancellationSubject: settings.cancellationSubject,
        cancellationTextBody: settings.cancellationTextBody,
        concurrencyToken: settings.concurrencyToken,
      }).then(mapSurgerySettings),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: surgeryKeys.settings() }),
  });

  return { update };
}
