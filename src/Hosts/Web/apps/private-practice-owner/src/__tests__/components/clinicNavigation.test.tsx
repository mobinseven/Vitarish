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

  it("resolves insurance settings to the owner settings route", () => {
    const { result } = renderHook(() => useClinicNavigation());

    act(() => result.current({ kind: "clinicSettingsInsurance" }));

    expect(Routes.Clinic.Settings.Insurance).toBe("/settings/insurance");
    expect(push).toHaveBeenCalledWith("/settings/insurance");
  });

  it("resolves surgery detail to the surgery route", () => {
    const { result } = renderHook(() => useClinicNavigation());

    act(() => result.current({ kind: "surgeryDetail", surgeryId: "s-1" }));

    expect(push).toHaveBeenCalledWith("/surgeries/s-1");
  });

  it("preselects the patient when planning a surgery from a patient", () => {
    const { result } = renderHook(() => useClinicNavigation());

    act(() => result.current({ kind: "surgeryNew", patientId: "patient-1" }));

    expect(push).toHaveBeenCalledWith("/surgeries/new?patientId=patient-1");
  });

  it("opens the plain surgery plan route without a patient", () => {
    const { result } = renderHook(() => useClinicNavigation());

    act(() => result.current({ kind: "surgeryNew" }));

    expect(push).toHaveBeenCalledWith("/surgeries/new");
  });

  it("resolves surgery type management to the owner settings route", () => {
    const { result } = renderHook(() => useClinicNavigation());

    act(() => result.current({ kind: "surgeryTypeManagement" }));

    expect(Routes.Clinic.Settings.SurgeryTypes).toBe("/settings/surgery-types");
    expect(push).toHaveBeenCalledWith("/settings/surgery-types");
  });
});
