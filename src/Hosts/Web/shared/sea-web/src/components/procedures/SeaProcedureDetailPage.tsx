"use client";

import { useState } from "react";
import { Button } from "@heroui/react";
import type { SeaNavigationIntent } from "../../navigation";
import { SeaLoadingState, SeaErrorState } from "../state/SeaStateSurface";
import { useSeaProcedureDetail } from "../../hooks/procedures/useSeaProcedureDetail";
import { SeaProcedureForm } from "./SeaProcedureForm";
import { SeaProcedureDeleteDialog } from "./SeaProcedureDeleteDialog";
import { SeaEventTimeline } from "../events/SeaEventTimeline";
import { SeaEventForm } from "../events/SeaEventForm";
import { useSeaEvents } from "../../hooks/events/useSeaEvents";
import { useLocalizedText } from "../../localization";

interface SeaProcedureDetailPageProps {
  projectId: string;
  procedureId: string;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaProcedureDetailPage({ projectId, procedureId, onNavigate }: SeaProcedureDetailPageProps) {
  const { t } = useLocalizedText();
  const { data: procedure, isLoading, error } = useSeaProcedureDetail(procedureId);
  const { data: events } = useSeaEvents(procedureId);

  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [isCreateEventOpen, setIsCreateEventOpen] = useState(false);

  if (isLoading) {
    return <SeaLoadingState label={t("sea.procedures.loading")} />;
  }

  if (error || !procedure) {
    return <SeaErrorState title={t("sea.procedures.notFound")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button
            variant="ghost"
            className="mb-2"
            onPress={() => onNavigate?.({ kind: "projectDetail", projectId })}
          >
            {t("sea.procedures.backToProject")}
          </Button>
          <h1 className="text-2xl font-bold">{procedure.name}</h1>
          {procedure.description && (
            <p className="text-sm text-muted">{procedure.description}</p>
          )}
        </div>
        <div className="flex gap-2">
          <Button variant="primary" onPress={() => setIsCreateEventOpen(true)}>
            {t("sea.events.newEvent")}
          </Button>
          <Button variant="secondary" onPress={() => setIsEditOpen(true)}>
            {t("sea.procedures.editProcedure")}
          </Button>
          <Button variant="danger" onPress={() => setIsDeleteOpen(true)}>
            {t("sea.procedures.deleteProcedure")}
          </Button>
        </div>
      </div>

      <SeaEventTimeline procedureId={procedureId} projectId={projectId} onNavigate={onNavigate} />

      <SeaProcedureForm
        isOpen={isEditOpen}
        onClose={() => setIsEditOpen(false)}
        projectId={projectId}
        procedure={procedure}
        onNavigate={onNavigate}
      />

      <SeaProcedureDeleteDialog
        isOpen={isDeleteOpen}
        onClose={() => setIsDeleteOpen(false)}
        procedure={procedure}
      />

      <SeaEventForm
        isOpen={isCreateEventOpen}
        onClose={() => setIsCreateEventOpen(false)}
        projectId={projectId}
        procedureId={procedureId}
        previousEventId={events && events.length > 0 ? events[events.length - 1].id : undefined}
        onNavigate={onNavigate}
      />
    </div>
  );
}
