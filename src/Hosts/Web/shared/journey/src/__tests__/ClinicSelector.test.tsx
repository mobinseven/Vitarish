import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  clinics: vi.fn(),
  clinicHandoff: vi.fn(),
  onboardingHandoff: vi.fn(),
  assign: vi.fn(),
}));

vi.mock("../api/generated/parties/parties", () => ({
  usePartiesGetSignInClinics: () => mocks.clinics(),
  partiesIssueSignInClinicHandoff: mocks.clinicHandoff,
  partiesIssueSignInOnboardingHandoff: mocks.onboardingHandoff,
}));

import { ClinicSelector } from "../features/clinicSelection/ClinicSelector";

// Without a message catalog, t() returns each key, so the keys name the controls.
const openButton = "journey.gates.workspace.openAction";
const createButton = "journey.gates.workspace.createAction";
const handoffPath = "/handoff";

function clinic(tenantId: string, name: string, hostname: string, role = "TenantOwner") {
  return { tenantId, name, hostname, role };
}

function loaded(clinics: ReturnType<typeof clinic>[]) {
  mocks.clinics.mockReturnValue({ data: { clinics }, isLoading: false, isError: false });
}

beforeEach(() => {
  Object.defineProperty(window, "location", {
    configurable: true,
    value: { href: "http://auth.localhost:3005/workspaces/select", assign: mocks.assign },
  });
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("ClinicSelector", () => {
  it("opens the only clinic by itself through a handoff link to its own host", async () => {
    loaded([clinic("t1", "Rezaei Clinic", "rezaei.localhost")]);
    mocks.clinicHandoff.mockResolvedValue({ hostname: "rezaei.localhost", token: "token-1" });

    render(<ClinicSelector handoffPath={handoffPath} />);

    await waitFor(() => expect(mocks.assign).toHaveBeenCalledWith("http://rezaei.localhost:3005/handoff#token-1"));
    expect(mocks.clinicHandoff).toHaveBeenCalledWith("t1");
    expect(mocks.onboardingHandoff).not.toHaveBeenCalled();
  });

  it("lists several clinics and opens the one chosen", async () => {
    loaded([clinic("t1", "First Clinic", "first.localhost"), clinic("t2", "Second Clinic", "second.localhost", "TenantUser")]);
    mocks.clinicHandoff.mockResolvedValue({ hostname: "second.localhost", token: "token-2" });

    render(<ClinicSelector handoffPath={handoffPath} />);
    expect(screen.getByText("First Clinic")).toBeTruthy();
    expect(screen.getByText("journey.gates.workspace.roles.staff")).toBeTruthy();
    expect(mocks.clinicHandoff).not.toHaveBeenCalled();

    await userEvent.click(screen.getAllByRole("button", { name: openButton })[1]);

    await waitFor(() => expect(mocks.assign).toHaveBeenCalledWith("http://second.localhost:3005/handoff#token-2"));
    expect(mocks.clinicHandoff).toHaveBeenCalledWith("t2");
  });

  it("sends a person with no clinic on to create one", async () => {
    loaded([]);
    mocks.onboardingHandoff.mockResolvedValue({ hostname: "start.localhost", token: "token-3" });

    render(<ClinicSelector handoffPath={handoffPath} />);

    await waitFor(() => expect(mocks.assign).toHaveBeenCalledWith("http://start.localhost:3005/handoff#token-3"));
  });

  it("says there is no clinic and offers no creation when this deployment cannot create one", async () => {
    loaded([]);
    mocks.onboardingHandoff.mockRejectedValue(new Error("404"));

    render(<ClinicSelector handoffPath={handoffPath} />);

    expect(await screen.findByText("journey.gates.workspace.noClinics")).toBeTruthy();
    expect(screen.queryByRole("button", { name: createButton })).toBeNull();
    expect(mocks.assign).not.toHaveBeenCalled();
  });

  it("reports a clinic that could not be opened and lets the person try again", async () => {
    loaded([clinic("t1", "First Clinic", "first.localhost"), clinic("t2", "Second Clinic", "second.localhost")]);
    mocks.clinicHandoff.mockRejectedValue(new Error("404"));

    render(<ClinicSelector handoffPath={handoffPath} />);
    await userEvent.click(screen.getAllByRole("button", { name: openButton })[0]);

    expect(await screen.findByText("journey.gates.workspace.openFailed")).toBeTruthy();
    expect(mocks.assign).not.toHaveBeenCalled();
    expect((screen.getAllByRole("button", { name: openButton })[0] as HTMLButtonElement).disabled).toBe(false);
  });
});
