import { useEffect, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  clinicEnsureDefaultSurgerySettings,
  clinicGetSurgerySettings,
} from "../../api/generated/clinic/clinic";
import { ApiError } from "../../api/client/apiFetch";
import { useLocalizedText, type Translate } from "../../localization";
import { mapSurgerySettings } from "../../models/surgeries/surgeryMappers";
import { surgeryKeys } from "./surgeryKeys";
import { useCanPlanSurgeries } from "./useCanPlanSurgeries";

/** The clinic's default cancellation message, described in the active language. */
function defaultSurgerySettingsBody(t: Translate) {
  return {
    cancellationSubject: t("clinic.surgeries.defaultSettings.cancellationSubject"),
    cancellationTextBody: t("clinic.surgeries.defaultSettings.cancellationText"),
  };
}

export function useSurgerySettings() {
  const { t } = useLocalizedText();
  const queryClient = useQueryClient();
  const canPlan = useCanPlanSurgeries();
  const ensured = useRef(false);

  const query = useQuery({
    queryKey: surgeryKeys.settings(),
    retry: false,
    queryFn: async () =>
      mapSurgerySettings(await clinicGetSurgerySettings()),
  });

  const ensure = useMutation({
    mutationFn: () =>
      clinicEnsureDefaultSurgerySettings(defaultSurgerySettingsBody(t)).then(
        mapSurgerySettings,
      ),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: surgeryKeys.settings() }),
  });

  const isMissing =
    query.error instanceof ApiError && query.error.status === 404;

  useEffect(() => {
    if (!isMissing || !canPlan || ensured.current || ensure.isPending) return;
    ensured.current = true;
    ensure.mutate(undefined, {
      onError: () => {
        ensured.current = false;
      },
    });
  }, [ensure, isMissing, canPlan]);

  return {
    settings: query.data ?? null,
    isLoading:
      query.isLoading ||
      ensure.isPending ||
      (query.isFetching && query.data === undefined),
    error: ensure.error ?? (canPlan && isMissing ? null : query.error),
    refetch: query.refetch,
  };
}
