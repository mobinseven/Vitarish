"use client";

import { useRouter } from "next/navigation";
import {
  SeaDashboardPage,
  SeaProjectForm,
  SeaProjectsPage,
  SeaRolesPage,
} from "@vitarish/sea";
import { SeaProcedureList } from "@vitarish/sea";
import { useSeaProjectDetail } from "@vitarish/sea";
import { Routes } from "@/app/routes";
import { PmEventWorkspace } from "./PmEventWorkspace";
import { PmProcedureWorkspace } from "./PmProcedureWorkspace";
import { PmProjectOverview } from "./PmProjectOverview";

export type SeaPage =
  | "overview"
  | "projects"
  | "project"
  | "projectNew"
  | "projectEdit"
  | "procedure"
  | "event"
  | "roles"
  | "statistics";

export function SeaRoutePage({
  page,
  projectId,
  procedureId,
  eventId,
  focusedEventId,
}: {
  page: SeaPage;
  projectId?: string;
  procedureId?: string;
  eventId?: string;
  focusedEventId?: string;
}) {
  const router = useRouter();
  const navigate = (
    intent: Parameters<
      NonNullable<React.ComponentProps<typeof SeaProjectsPage>["onNavigate"]>
    >[0],
  ) => {
    switch (intent.kind) {
      case "projectList":
        return router.push(Routes.Projects);
      case "projectNew":
        return router.push(Routes.ProjectNew);
      case "projectDetail":
        return router.push(Routes.Project(intent.projectId));
      case "projectEdit":
        return router.push(Routes.ProjectEdit(intent.projectId));
      case "procedureDetail":
        return router.push(
          Routes.Procedure(intent.projectId, intent.procedureId),
        );
      case "eventDetail":
        return router.push(
          Routes.Event(intent.projectId, intent.procedureId, intent.eventId),
        );
      default:
        return router.push(Routes.Root);
    }
  };

  if (page === "overview" || page === "statistics") return <SeaDashboardPage />;
  if (page === "projects") return <SeaProjectsPage onNavigate={navigate} />;
  if (page === "projectNew")
    return (
      <SeaProjectForm
        isOpen
        onClose={() => router.push(Routes.Projects)}
        onNavigate={navigate}
      />
    );
  if (page === "projectEdit" && projectId)
    return <ProjectEditRoute projectId={projectId} onNavigate={navigate} />;
  if (page === "project" && projectId)
    return <PmProjectOverview projectId={projectId} />;
  if (page === "procedure" && projectId && procedureId)
    return (
      <PmProcedureWorkspace
        projectId={projectId}
        procedureId={procedureId}
        focusedEventId={focusedEventId}
      />
    );
  if (page === "event" && projectId && procedureId && eventId)
    return (
      <PmEventWorkspace
        projectId={projectId}
        procedureId={procedureId}
        eventId={eventId}
      />
    );
  if (page === "roles") return <SeaRolesPage />;
  return <SeaProcedureList projectId={projectId ?? ""} onNavigate={navigate} />;
}

function ProjectEditRoute({
  projectId,
  onNavigate,
}: {
  projectId: string;
  onNavigate: NonNullable<
    React.ComponentProps<typeof SeaProjectsPage>["onNavigate"]
  >;
}) {
  const router = useRouter();
  const { data: project } = useSeaProjectDetail(projectId);
  return (
    <SeaProjectForm
      isOpen={!!project}
      project={project}
      onClose={() => router.push(Routes.Project(projectId))}
      onNavigate={onNavigate}
    />
  );
}
