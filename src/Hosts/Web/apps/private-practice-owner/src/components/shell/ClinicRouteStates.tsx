"use client";

import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { EmptyStateSurface, ErrorStateSurface } from "@vitarish/ui";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

/** Shown inside the clinic shell when a URL matches no page, so the sidebar stays available. */
export function ClinicNotFoundState() {
  const { t } = useLocalizedText();
  const router = useRouter();

  return (
    <EmptyStateSurface
      title={t("routeStates.notFoundTitle")}
      description={t("routeStates.notFoundDescription")}
    >
      <div className="flex flex-wrap justify-center gap-2">
        <Button onPress={() => router.push(Routes.Clinic.Index)}>
          {t("routeStates.goToToday")}
        </Button>
        <Button variant="ghost" onPress={() => router.push(Routes.Clinic.Patients.Index)}>
          {t("routeStates.goToPatients")}
        </Button>
      </div>
    </EmptyStateSurface>
  );
}

/** Shown inside the clinic shell when a page throws while rendering. */
export function ClinicRouteErrorState({ reset }: { reset: () => void }) {
  const { t } = useLocalizedText();
  const router = useRouter();

  return (
    <ErrorStateSurface
      title={t("routeStates.errorTitle")}
      description={t("routeStates.errorDescription")}
    >
      <div className="mt-3 flex flex-wrap gap-2">
        <Button onPress={reset}>{t("common.actions.retry")}</Button>
        <Button variant="ghost" onPress={() => router.push(Routes.Clinic.Index)}>
          {t("routeStates.goToToday")}
        </Button>
      </div>
    </ErrorStateSurface>
  );
}
