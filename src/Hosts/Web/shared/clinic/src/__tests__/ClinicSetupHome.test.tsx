import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ClinicSetupGoal, type ClinicSetupProgressResponse } from "../api/generated/clinic/model";
import { ClinicSetupHome } from "../features/clinic-setup/ClinicSetupHome";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const dismiss = vi.fn();
const submit = vi.fn();
vi.mock("../api/generated/clinic/clinic", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../api/generated/clinic/clinic")>()),
  useClinicDismissSetupGuide: () => ({ mutateAsync: dismiss, isPending: false }),
}));
vi.mock("../features/clinic-assistant/OperationsAssistantAside", () => ({
  OperationsAssistantAside: ({ welcome }: { welcome?: (send: (message: string) => void) => ReactNode }) => (
    <div data-testid="assistant">{welcome?.(submit)}</div>
  ),
}));

function Wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={client}>
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>{children}</LocalizationProvider>
    </QueryClientProvider>
  );
}

const progress: ClinicSetupProgressResponse = {
  applies: true,
  dismissed: false,
  nextGoal: ClinicSetupGoal.ShiftsAndBookingRules,
  goals: [
    { goal: ClinicSetupGoal.ServicesAndProviders, done: true },
    { goal: ClinicSetupGoal.ShiftsAndBookingRules, done: false },
    { goal: ClinicSetupGoal.StaffUsers, done: false },
    { goal: ClinicSetupGoal.ClinicProfile, done: false },
  ],
};

describe("ClinicSetupHome", () => {
  beforeEach(() => {
    dismiss.mockReset();
    submit.mockReset();
  });
  afterEach(cleanup);

  it("shows the goals and the welcome for the next goal", () => {
    render(<ClinicSetupHome progress={progress} onNavigate={vi.fn()} />, { wrapper: Wrapper });

    expect(screen.getByText("Services and providers")).toBeInTheDocument();
    expect(screen.getByText("Shifts and booking rules")).toBeInTheDocument();
    expect(screen.getByText("Staff users")).toBeInTheDocument();
    expect(screen.getByText("Clinic profile and logo")).toBeInTheDocument();
    expect(screen.getByText(clinicMessageCatalogs.en.clinic.setup.welcome.ShiftsAndBookingRules)).toBeInTheDocument();
  });

  it("offers a chip only for open goals and sends its label as a message", () => {
    const { chips } = clinicMessageCatalogs.en.clinic.setup;
    render(<ClinicSetupHome progress={progress} onNavigate={vi.fn()} />, { wrapper: Wrapper });

    expect(screen.queryByRole("button", { name: chips.ServicesAndProviders })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: chips.ShiftsAndBookingRules }));

    expect(submit).toHaveBeenCalledWith(chips.ShiftsAndBookingRules);
  });

  it("welcomes a finished setup without chips", () => {
    const { welcome, chips } = clinicMessageCatalogs.en.clinic.setup;
    render(
      <ClinicSetupHome
        progress={{ ...progress, nextGoal: null, goals: progress.goals.map((goal) => ({ ...goal, done: true })) }}
        onNavigate={vi.fn()}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.getByText(welcome.done)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: chips.StaffUsers })).toBeNull();
  });

  it("dismisses the guide on Later", async () => {
    dismiss.mockResolvedValue(undefined);
    render(<ClinicSetupHome progress={progress} onNavigate={vi.fn()} />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: "Later" }));

    await waitFor(() => expect(dismiss).toHaveBeenCalledTimes(1));
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("shows an error when dismissing fails", async () => {
    dismiss.mockRejectedValue(new Error("offline"));
    render(<ClinicSetupHome progress={progress} onNavigate={vi.fn()} />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: "Later" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(clinicMessageCatalogs.en.clinic.setup.laterError);
  });

  it.each([
    ["Shifts and booking rules", { kind: "ownerShiftIndex" }],
    ["Staff users", { kind: "clinicSettingsUsers" }],
    ["Clinic profile and logo", { kind: "clinicSettings" }],
  ])("opens the screen of the open goal %s", (label, intent) => {
    const onNavigate = vi.fn();
    render(<ClinicSetupHome progress={progress} onNavigate={onNavigate} />, { wrapper: Wrapper });

    fireEvent.click(screen.getByRole("button", { name: new RegExp(label) }));

    expect(onNavigate).toHaveBeenCalledWith(intent);
  });

  it("opens the services screen for the services goal and leaves a done goal unselectable", () => {
    const onNavigate = vi.fn();
    const open = {
      ...progress,
      goals: progress.goals.map((goal) => ({ ...goal, done: false })),
      nextGoal: ClinicSetupGoal.ServicesAndProviders,
    };
    const { unmount } = render(<ClinicSetupHome progress={progress} onNavigate={onNavigate} />, { wrapper: Wrapper });
    expect(screen.queryByRole("button", { name: /^Services and providers/ })).toBeNull();
    unmount();

    render(<ClinicSetupHome progress={open} onNavigate={onNavigate} />, { wrapper: Wrapper });
    fireEvent.click(screen.getByRole("button", { name: /^Services and providers/ }));

    expect(onNavigate).toHaveBeenCalledWith({ kind: "serviceIndex" });
  });
});
