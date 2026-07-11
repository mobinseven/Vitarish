import { describe, expect, it } from "vitest";
import type {
  ProjectReadDto,
  DatabaseStatisticsReadDto,
} from "../api/generated/sea/model";
import {
  mapSeaInteger,
  mapSeaProject,
  mapSeaStatistics,
  mapSeaUserRole,
} from "../features/sea/shared/models/seaReadDtoMappers";

function createProject(overrides: Partial<ProjectReadDto> = {}): ProjectReadDto {
  return {
    id: "project-1",
    name: "Project",
    description: "Description",
    avatarFileId: null,
    identificationCode: null,
    template: false,
    stats: {
      state: null,
      procedures: "2",
      events: "4",
      committedEvents: 3,
      eventItems: "8",
      newItems: "1",
      unread: true,
      lastUpdate: null,
    },
    userIds: ["user-1"],
    createdAt: "2026-07-07T00:00:00Z",
    createdBy: null,
    updatedAt: null,
    updatedBy: null,
    deletedAt: null,
    deletedBy: null,
    isDeleted: false,
    metadata: undefined,
    metadataValues: undefined,
    ...overrides,
  };
}

describe("SEA read DTO mappers", () => {
  it("maps string-encoded integer fields to numbers", () => {
    const project = mapSeaProject(createProject());

    expect(project.id).toBe("project-1");
    expect(project.stats?.procedures).toBe(2);
    expect(project.stats?.events).toBe(4);
    expect(project.stats?.committedEvents).toBe(3);
    expect(project.stats?.eventItems).toBe(8);
    expect(project.stats?.newItems).toBe(1);
  });

  it("rejects missing required tracked fields", () => {
    expect(() => mapSeaProject(createProject({ id: undefined }))).toThrow(
      "SEA contract violation: project.id is required."
    );
  });

  it("rejects empty integer strings", () => {
    expect(() => mapSeaInteger("   ", "statistics.events")).toThrow(
      "SEA contract violation: statistics.events must be a valid integer string."
    );
  });

  it("rejects non-integer numeric values", () => {
    expect(() => mapSeaInteger("2.5", "statistics.events")).toThrow(
      "SEA contract violation: statistics.events must be an integer."
    );
  });

  it("rejects null DTOs with contract-specific errors", () => {
    expect(() => mapSeaProject(null as unknown as ProjectReadDto)).toThrow(
      "SEA contract violation: project DTO is required."
    );
    expect(() => mapSeaUserRole(null as never)).toThrow(
      "SEA contract violation: userRole DTO is required."
    );
    expect(() => mapSeaStatistics(null as unknown as DatabaseStatisticsReadDto)).toThrow(
      "SEA contract violation: statistics DTO is required."
    );
  });
});
