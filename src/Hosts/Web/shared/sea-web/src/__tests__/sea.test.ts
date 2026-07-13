import { describe, expect, it } from "vitest";
import {
  SeaErrorState,
  SeaLoadingState,
  SeaProjectDetailPage,
  SeaProjectsPage,
  SeaRolesPage,
  useCreateSeaProject,
  useSeaProjectDetail,
  useSeaProjects,
} from "../index";

describe("sea-web barrel exports", () => {
  it("exports key components and hooks", () => {
    expect(SeaProjectsPage).toBeDefined();
    expect(SeaProjectDetailPage).toBeDefined();
    expect(SeaRolesPage).toBeDefined();
    expect(SeaLoadingState).toBeDefined();
    expect(SeaErrorState).toBeDefined();
    expect(useSeaProjects).toBeDefined();
    expect(useSeaProjectDetail).toBeDefined();
    expect(useCreateSeaProject).toBeDefined();
  });
});
