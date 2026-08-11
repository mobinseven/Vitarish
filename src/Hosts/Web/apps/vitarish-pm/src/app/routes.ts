export const Routes = {
  Root: "/",
  Projects: "/projects",
  Project: (id: string) => `/projects/${id}`,
  ProjectEdit: (id: string) => `/projects/${id}/edit`,
  ProjectNew: "/projects/new",
  Procedure: (projectId: string, procedureId: string) =>
    `/projects/${projectId}/procedures/${procedureId}`,
  Event: (projectId: string, procedureId: string, eventId: string) =>
    `/projects/${projectId}/procedures/${procedureId}/events/${eventId}`,
  Roles: "/settings/roles",
  Statistics: "/settings/statistics",
} as const;
