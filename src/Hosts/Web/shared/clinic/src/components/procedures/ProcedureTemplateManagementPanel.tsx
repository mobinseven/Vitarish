"use client";

import { useEffect, useState } from "react";
import { Alert, Button, Spinner, Tooltip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import {
  useSeaEvents,
  useSeaProcedures,
  useSeaTemplateProjects,
} from "@vitarish/sea";
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
  // Narrow screens show one pane at a time: the category list until a category is opened.
  const [isProjectOpen, setIsProjectOpen] = useState(false);
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

  const openProject = (id: string) => {
    setProjectId(id);
    setIsProjectOpen(true);
  };
  const backLabel = t("clinic.procedures.templateManagement.projects");

  return (
    <div className="flex h-full min-h-0 gap-3">
      <div className={`${isProjectOpen ? "hidden" : "flex"} min-h-0 w-full md:flex md:w-72 md:shrink-0`}>
        <Widget className="w-full overflow-auto">
          <TemplateProjectListPane
            projects={projects}
            selectedProjectId={projectId}
            onSelect={openProject}
          />
        </Widget>
      </div>
      <div className={`${isProjectOpen ? "flex" : "hidden"} min-h-0 min-w-0 flex-1 flex-col items-start gap-3 md:flex`}>
        <Tooltip>
          <Button
            className="md:hidden"
            variant="tertiary"
            onPress={() => setIsProjectOpen(false)}
          >
            <FaIcon icon="arrow-left" aria-hidden="true" className="rtl:rotate-180" />
            {backLabel}
          </Button>
          <Tooltip.Content>{backLabel}</Tooltip.Content>
        </Tooltip>
        <Widget className="w-full min-w-0 flex-1 overflow-auto">
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
    </div>
  );
}
