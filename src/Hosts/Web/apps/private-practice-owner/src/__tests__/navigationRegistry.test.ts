import { describe, expect, it } from "vitest";
import {
  clinicOwnerDestinations,
  clinicUserDestinations,
  destinationForPath,
} from "@/app/navigationRegistry";
import { Routes } from "@/app/routes";

/** Every concrete path the clinic route tree can produce, with sample ids for the builders. */
function clinicPaths(node: unknown): string[] {
  if (typeof node === "string") return [node];
  if (typeof node === "function") {
    const build = node as (...args: string[]) => string;
    return [build(...Array.from({ length: build.length }, () => "sample"))];
  }
  if (node && typeof node === "object") return Object.values(node).flatMap(clinicPaths);
  return [];
}

describe("navigation registry", () => {
  it("gives every destination a unique id and address", () => {
    for (const destinations of [clinicOwnerDestinations, clinicUserDestinations]) {
      expect(new Set(destinations.map((d) => d.id)).size).toBe(destinations.length);
      expect(new Set(destinations.map((d) => d.href)).size).toBe(destinations.length);
    }
  });

  it("places every clinic owner route under exactly one destination", () => {
    const paths = clinicPaths(Routes.Clinic).map((path) => path.split("?")[0]);
    expect(paths.length).toBeGreaterThan(20);

    for (const path of paths) {
      const matches = clinicOwnerDestinations.filter((d) =>
        d.owns.some((prefix) => path === prefix || path.startsWith(`${prefix}/`)),
      );
      expect(matches, path).toHaveLength(1);
      expect(destinationForPath(path, clinicOwnerDestinations)).toBe(matches[0]);
    }
  });

  it("keeps a destination highlighted on its child pages", () => {
    expect(destinationForPath("/patients/abc/procedures/def", clinicOwnerDestinations)?.id).toBe("patients");
    expect(destinationForPath("/settings/users", clinicOwnerDestinations)?.id).toBe("settings");
  });

  it("places a clinic user's shift and reservation pages under their own destination", () => {
    expect(destinationForPath("/settings/shifts/abc", clinicUserDestinations)?.id).toBe("shifts");
    expect(destinationForPath("/appointments/abc", clinicUserDestinations)?.id).toBe("reservations");
  });

  it("does not highlight anything outside the sidebar", () => {
    expect(destinationForPath("/account/security", clinicOwnerDestinations)).toBeUndefined();
    expect(destinationForPath("/patients-archive", clinicOwnerDestinations)).toBeUndefined();
  });
});
