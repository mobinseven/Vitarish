import { describe, expect, it } from "vitest";

describe("sea-web barrel exports", () => {
  it("exports key components and hooks", async () => {
    const mod = await import("../index");
    expect(mod.SeaProjectsPage).toBeDefined();
    expect(mod.SeaProjectDetailPage).toBeDefined();
    expect(mod.SeaRolesPage).toBeDefined();
    expect(mod.SeaLoadingState).toBeDefined();
    expect(mod.SeaErrorState).toBeDefined();
    expect(mod.useSeaProjects).toBeDefined();
    expect(mod.useSeaProjectDetail).toBeDefined();
    expect(mod.useCreateSeaProject).toBeDefined();
  }, 10_000);
});
