import { describe, expect, it } from "vitest";
import { Routes } from "@/app/routes";

describe("SEA routes", () => {
  it("builds owner SEA project routes from the central route table", () => {
    expect(Routes.Sea.Index).toBe("/owner/sea");
    expect(Routes.Sea.Projects.Index).toBe("/owner/sea/projects");
    expect(Routes.Sea.Projects.New).toBe("/owner/sea/projects/new");
    expect(Routes.Sea.Projects.Detail("project-1")).toBe("/owner/sea/projects/project-1");
    expect(Routes.Sea.Projects.Edit("project-1")).toBe("/owner/sea/projects/project-1/edit");
  });

  it("builds nested SEA procedure and event routes with explicit route params", () => {
    expect(Routes.Sea.Procedures.Detail("project-1", "procedure-2")).toBe(
      "/owner/sea/projects/project-1/procedures/procedure-2"
    );
    expect(Routes.Sea.Events.Detail("project-1", "procedure-2", "event-3")).toBe(
      "/owner/sea/projects/project-1/procedures/procedure-2/events/event-3"
    );
  });

  it("builds template and settings routes from the SEA route owner", () => {
    expect(Routes.Sea.Templates.Index).toBe("/owner/sea/templates");
    expect(Routes.Sea.Templates.Detail("template-1")).toBe("/owner/sea/templates/template-1");
    expect(Routes.Sea.Settings.Users).toBe("/owner/sea/settings/users");
    expect(Routes.Sea.Settings.Statistics).toBe("/owner/sea/settings/statistics");
  });
});
