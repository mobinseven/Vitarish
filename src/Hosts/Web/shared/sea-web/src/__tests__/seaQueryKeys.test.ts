import { describe, expect, it } from "vitest";
import { seaAttachmentKeys } from "../hooks/attachments/seaAttachmentKeys";
import { seaEventKeys } from "../hooks/events/seaEventKeys";
import { seaProcedureKeys } from "../hooks/procedures/seaProcedureKeys";
import { seaProjectKeys } from "../hooks/projects/seaProjectKeys";
import { seaRoleKeys } from "../hooks/roles/seaRoleKeys";
import { seaRoleUserKeys } from "../hooks/roles/useSeaRoleUsers";
import { seaTemplateKeys } from "../hooks/templates/seaTemplateKeys";

describe("SEA query keys", () => {
  it("keeps project and procedure server-state keys under SEA ownership", () => {
    expect(seaProjectKeys.all).toEqual(["sea", "projects"]);
    expect(seaProjectKeys.list()).toEqual(["sea", "projects", "list"]);
    expect(seaProjectKeys.detail("project-1")).toEqual(["sea", "projects", "detail", "project-1"]);
    expect(seaProjectKeys.procedures("project-1")).toEqual(["sea", "projects", "procedures", "project-1"]);

    expect(seaProcedureKeys.all).toEqual(["sea", "procedures"]);
    expect(seaProcedureKeys.list("project-1")).toEqual(["sea", "procedures", "list", "project-1"]);
    expect(seaProcedureKeys.detail("procedure-1")).toEqual(["sea", "procedures", "detail", "procedure-1"]);
    expect(seaProcedureKeys.events("procedure-1")).toEqual(["sea", "procedures", "events", "procedure-1"]);
  });

  it("keeps event, attachment, role, and template keys stable", () => {
    expect(seaEventKeys.list("procedure-1")).toEqual(["sea", "events", "list", "procedure-1"]);
    expect(seaEventKeys.detail("event-1")).toEqual(["sea", "events", "detail", "event-1"]);
    expect(seaEventKeys.writePermission("event-1")).toEqual(["sea", "events", "writePermission", "event-1"]);

    expect(seaAttachmentKeys.list("event-1")).toEqual(["sea", "attachments", "list", "event-1"]);
    expect(seaAttachmentKeys.count("event-1")).toEqual(["sea", "attachments", "count", "event-1"]);

    expect(seaRoleKeys.list()).toEqual(["sea", "roles", "list"]);
    expect(seaRoleKeys.userRoles("user-1")).toEqual(["sea", "roles", "userRoles", "user-1"]);
    expect(seaRoleKeys.eventReadRoles("event-1")).toEqual(["sea", "roles", "eventReadRoles", "event-1"]);
    expect(seaRoleKeys.eventWriteRoles("event-1")).toEqual(["sea", "roles", "eventWriteRoles", "event-1"]);
    expect(seaRoleUserKeys.search(" user ")).toEqual(["sea", "role-users", "search", "user"]);

    expect(seaTemplateKeys.list()).toEqual(["sea", "templates", "list"]);
    expect(seaTemplateKeys.detail("template-1")).toEqual(["sea", "templates", "detail", "template-1"]);
  });
});
