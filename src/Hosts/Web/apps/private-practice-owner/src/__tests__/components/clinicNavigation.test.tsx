import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Routes } from "@/app/routes";
import { useClinicNavigation } from "@/components/clinic-navigation";

const push = vi.hoisted(() => vi.fn());

vi.mock("next/navigation", () => ({ useRouter: () => ({ push }) }));

describe("useClinicNavigation", () => {
  beforeEach(() => push.mockClear());

  it("resolves procedure-template management to the owner settings route", () => {
    const { result } = renderHook(() => useClinicNavigation());

    act(() => result.current({ kind: "procedureTemplateManagement" }));

    expect(Routes.Clinic.Settings.ProcedureTemplates).toBe(
      "/settings/procedure-templates",
    );
    expect(push).toHaveBeenCalledWith("/settings/procedure-templates");
  });
});
