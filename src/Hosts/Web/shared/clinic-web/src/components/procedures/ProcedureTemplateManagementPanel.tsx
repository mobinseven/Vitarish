"use client";

import { useEffect, useState } from "react";
import { Alert, Spinner } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import {
  useSeaEvents,
  useSeaProcedures,
  useSeaTemplateProjects,
} from "@vitarish/sea-web";
import { useLocalizedText } from "../../localization";
import { TemplateProjectListPane } from "./TemplateProjectListPane";
import { TemplateProjectDetailPane } from "./TemplateProjectDetailPane";

export function ProcedureTemplateManagementPanel() {
  const { t } = useLocalizedText();
  const projectsQuery = useSeaTemplateProjects();
  const projects = projectsQuery.data ?? [];
  const [projectId, setProjectId] = useState<string | null>(null);
  const [procedureId, setProcedureId] = useState<string | null>(null);
  const [eventId, setEventId] = useState<string | null>(null);
  const proceduresQuery = useSeaProcedures(projectId ?? "");
  const procedures = proceduresQuery.data ?? [];
  const eventsQuery = useSeaEvents(procedureId ?? "");
  const events = eventsQuery.data ?? [];

  useEffect(() => {
    setProjectId((id) =>
      projects.some((project) => project.id === id)
        ? id
        : (projects[0]?.id ?? null),
    );
  }, [projects]);
  useEffect(() => {
    setProcedureId((id) =>
      procedures.some((procedure) => procedure.id === id)
        ? id
        : (procedures[0]?.id ?? null),
    );
  }, [procedures]);
  useEffect(() => {
    setEventId((id) =>
      events.some((event) => event.id === id) ? id : (events[0]?.id ?? null),
    );
  }, [events]);

  if (projectsQuery.isLoading)
    return <Spinner aria-label={t("common.states.loading")} />;
  if (projectsQuery.error)
    return (
      <Alert status="danger">
        <Alert.Indicator />
        <Alert.Content>
          <Alert.Title>
            {t("clinic.procedures.templateManagement.loadFailed")}
          </Alert.Title>
        </Alert.Content>
      </Alert>
    );

  return (
    <div className="flex h-full min-h-0 gap-3">
      <Widget className="w-72 shrink-0 overflow-auto">
        <TemplateProjectListPane
          projects={projects}
          selectedProjectId={projectId}
          onSelect={setProjectId}
        />
      </Widget>
      <Widget className="min-w-0 flex-1 overflow-auto">
        <TemplateProjectDetailPane
          projectId={projectId}
          procedures={procedures}
          selectedProcedureId={procedureId}
          onSelectProcedure={setProcedureId}
          events={events}
          selectedEventId={eventId}
          onSelectEvent={setEventId}
        />
      </Widget>
    </div>
  );
}
