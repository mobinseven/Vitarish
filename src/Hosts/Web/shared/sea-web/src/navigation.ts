export type SeaNavigationIntent =
  | { kind: "projectList" }
  | { kind: "projectDetail"; projectId: string }
  | { kind: "projectNew" }
  | { kind: "projectEdit"; projectId: string }
  | { kind: "procedureDetail"; projectId: string; procedureId: string }
  | { kind: "eventDetail"; projectId: string; procedureId: string; eventId: string }
  | { kind: "templateList" }
  | { kind: "templateDetail"; projectId: string }
  | { kind: "settingsUsers" }
  | { kind: "settingsStatistics" };
