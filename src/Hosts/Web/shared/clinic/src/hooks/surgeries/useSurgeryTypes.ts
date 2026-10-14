import { useEffect, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  clinicEnsureDefaultSurgeryCatalog,
  clinicGetSurgeryCatalogs,
} from "../../api/generated/clinic/clinic";
import { useLocalizedText, type Translate } from "../../localization";
import { mapSurgeryType } from "../../models/surgeries/surgeryMappers";
import { surgeryKeys } from "./surgeryKeys";
import { useCanPlanSurgeries } from "./useCanPlanSurgeries";

/** The clinic's default surgery type, described in the active language. */
function defaultSurgeryTypeBody(t: Translate) {
  return {
    name: t("clinic.surgeries.defaultType.name"),
    tools: [],
    defaultTeamRoles: [
      t("clinic.surgeries.defaultType.roleSurgeon"),
      t("clinic.surgeries.defaultType.roleAssistant"),
      t("clinic.surgeries.defaultType.roleAnesthetist"),
    ],
    reminderPolicies: [
      {
        name: t("clinic.surgeries.defaultType.dayBeforeName"),
        minutesBefore: 1440,
        subject: t("clinic.surgeries.defaultType.dayBeforeSubject"),
        textBody: t("clinic.surgeries.defaultType.dayBeforeText"),
      },
      {
        name: t("clinic.surgeries.defaultType.hoursBeforeName"),
        minutesBefore: 120,
        subject: t("clinic.surgeries.defaultType.hoursBeforeSubject"),
        textBody: t("clinic.surgeries.defaultType.hoursBeforeText"),
      },
    ],
  };
}

export function useSurgeryTypes() {
  const { t } = useLocalizedText();
  const queryClient = useQueryClient();
  const canPlan = useCanPlanSurgeries();
  const ensured = useRef(false);

  const query = useQuery({
    queryKey: surgeryKeys.types(),
    queryFn: async () => (await clinicGetSurgeryCatalogs()).map(mapSurgeryType),
  });

  const ensure = useMutation({
    mutationFn: () =>
      clinicEnsureDefaultSurgeryCatalog(defaultSurgeryTypeBody(t)).then(
        mapSurgeryType,
      ),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: surgeryKeys.types() }),
  });

  const hasEmptyList = query.isSuccess && (query.data?.length ?? 0) === 0;

  useEffect(() => {
    if (!canPlan || !hasEmptyList || ensured.current || ensure.isPending) return;
    ensured.current = true;
    ensure.mutate(undefined, {
      onError: () => {
        ensured.current = false;
      },
    });
  }, [ensure, canPlan, hasEmptyList]);

  return {
    types: query.data ?? [],
    isLoading: query.isLoading || ensure.isPending,
    error: query.error ?? ensure.error,
    refetch: query.refetch,
  };
}
