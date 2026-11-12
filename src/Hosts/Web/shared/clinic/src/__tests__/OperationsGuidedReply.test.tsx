import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useClinicGetSetupProgress } from "../api/generated/clinic/clinic";
import { ClinicSetupGoal, type ClinicSetupProgressResponse } from "../api/generated/clinic/model";
import { OperationsGuidedReply, type OperationsGuidedReplyProps } from "../features/clinic-assistant/OperationsGuidedReply";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

vi.mock("../api/generated/clinic/clinic", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../api/generated/clinic/clinic")>()),
  useClinicGetSetupProgress: vi.fn(),
}));
vi.mock("../features/clinic-assistant/AssistantActions", () => ({
  OperationsActionGroup: ({ actionIds }: { actionIds: string[] }) => <div data-testid="action-group">{actionIds.join(",")}</div>,
}));

const progress = (overrides: Partial<ClinicSetupProgressResponse>): ClinicSetupProgressResponse => ({
  applies: true,
  dismissed: true,
  nextGoal: ClinicSetupGoal.ShiftsAndBookingRules,
  goals: [
    { goal: ClinicSetupGoal.ServicesAndProviders, done: true },
    { goal: ClinicSetupGoal.ShiftsAndBookingRules, done: false },
    { goal: ClinicSetupGoal.StaffUsers, done: false },
    { goal: ClinicSetupGoal.ClinicProfile, done: false },
  ],
  ...overrides,
});

const mockProgress = (data: ClinicSetupProgressResponse | undefined) =>
  vi.mocked(useClinicGetSetupProgress).mockReturnValue({ data } as unknown as ReturnType<typeof useClinicGetSetupProgress>);

function Wrapper({ children }: { children: ReactNode }) {
  return <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>{children}</LocalizationProvider>;
}

const goalLabels = clinicMessageCatalogs.en.clinic.setup.goals;

function renderReply(props: Partial<OperationsGuidedReplyProps> = {}, wrapper = Wrapper) {
  const submit = vi.fn();
  const onOpenGoal = vi.fn();
  render(
    <OperationsGuidedReply
      actionGroups={[]}
      question={null}
      nextAction={null}
      turnKey="turn-1"
      isPending={false}
      submit={submit}
      replaceAction={vi.fn()}
      onOpenGoal={onOpenGoal}
      {...props}
    />,
    { wrapper },
  );
  return { submit, onOpenGoal };
}

describe("OperationsGuidedReply", () => {
  beforeEach(() => {
    vi.mocked(useClinicGetSetupProgress).mockReset();
    mockProgress(undefined);
  });
  afterEach(cleanup);

  it("lists the setup goals after the owner chose Later and opens the selected open goal", () => {
    mockProgress(progress({}));
    const { onOpenGoal } = renderReply({ showDismissedSetupGoals: true });

    expect(screen.getAllByRole("listitem")).toHaveLength(4);
    expect(screen.queryByRole("button", { name: new RegExp(goalLabels.ServicesAndProviders) })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: new RegExp(goalLabels.StaffUsers) }));

    expect(onOpenGoal).toHaveBeenCalledTimes(1);
    expect(onOpenGoal).toHaveBeenCalledWith(ClinicSetupGoal.StaffUsers);
  });

  it("only reads setup progress when the dismissed goals are requested", () => {
    renderReply();
    expect(vi.mocked(useClinicGetSetupProgress).mock.calls[0][0]?.query).toMatchObject({ enabled: false });
    cleanup();
    mockProgress(progress({}));
    renderReply({ showDismissedSetupGoals: true });
    expect(vi.mocked(useClinicGetSetupProgress).mock.calls.at(-1)?.[0]?.query).toMatchObject({ enabled: true });
  });

  it("shows no goal list while the setup guide is not dismissed", () => {
    mockProgress(progress({ dismissed: false }));
    renderReply({ showDismissedSetupGoals: true });
    expect(screen.queryByRole("navigation")).toBeNull();
    expect(screen.queryByText(goalLabels.StaffUsers)).toBeNull();
  });

  it("shows no goal list when every goal is done", () => {
    mockProgress(progress({
      nextGoal: null as unknown as ClinicSetupProgressResponse["nextGoal"],
      goals: Object.values(ClinicSetupGoal).map((goal) => ({ goal, done: true })),
    }));
    renderReply({ showDismissedSetupGoals: true });
    expect(screen.queryByRole("navigation")).toBeNull();
  });

  it("shows no goal list without showDismissedSetupGoals or without an open-goal handler", () => {
    mockProgress(progress({}));
    renderReply({ showDismissedSetupGoals: false });
    expect(screen.queryByRole("navigation")).toBeNull();
    cleanup();
    renderReply({ showDismissedSetupGoals: true, onOpenGoal: undefined });
    expect(screen.queryByRole("navigation")).toBeNull();
  });

  it("submits the labels of the chosen options as the answer", () => {
    const options = [{ key: "a", label: "Dental" }, { key: "b", label: "Physiotherapy" }, { key: "c", label: "Psychology" }];
    const { submit } = renderReply({ question: { prompt: "Which kinds?", options, maxSelections: 2 } });

    fireEvent.click(screen.getByRole("checkbox", { name: "Dental" }));
    fireEvent.click(screen.getByRole("checkbox", { name: "Psychology" }));
    fireEvent.click(screen.getByRole("button"));

    expect(submit).toHaveBeenCalledTimes(1);
    expect(submit).toHaveBeenCalledWith("Dental, Psychology");
  });

  it("joins multiple answers with the Persian separator in Persian", () => {
    const options = [{ key: "a", label: "یک" }, { key: "b", label: "دو" }];
    const { submit } = renderReply(
      { question: { prompt: "کدام؟", options, maxSelections: 2 } },
      ({ children }) => <LocalizationProvider locale="fa" messages={clinicMessageCatalogs.fa}>{children}</LocalizationProvider>,
    );

    fireEvent.click(screen.getByRole("checkbox", { name: "یک" }));
    fireEvent.click(screen.getByRole("checkbox", { name: "دو" }));
    fireEvent.click(screen.getByRole("button"));

    expect(submit).toHaveBeenCalledWith("یک، دو");
  });

  it("does not ask the question while a reply is pending", () => {
    renderReply({ isPending: true, question: { prompt: "Which?", options: [{ key: "a", label: "A" }, { key: "b", label: "B" }], maxSelections: 1 } });
    expect(screen.queryByRole("radio")).toBeNull();
  });

  it("opens the next goal's screen from the next-step button", () => {
    const { onOpenGoal } = renderReply({ nextAction: ClinicSetupGoal.ShiftsAndBookingRules });

    fireEvent.click(screen.getByRole("button", { name: `Open ${goalLabels.ShiftsAndBookingRules}` }));

    expect(onOpenGoal).toHaveBeenCalledWith(ClinicSetupGoal.ShiftsAndBookingRules);
  });

  it("offers no next-step button without a handler or while pending", () => {
    renderReply({ nextAction: ClinicSetupGoal.StaffUsers, onOpenGoal: undefined });
    expect(screen.queryByRole("button")).toBeNull();
    cleanup();
    renderReply({ nextAction: ClinicSetupGoal.StaffUsers, isPending: true });
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("renders one review group per grouped action set", () => {
    renderReply({
      actionGroups: [
        { agentRunId: "run-1", actionIds: ["a1", "a2"] },
        { agentRunId: "run-1", actionIds: ["b1"] },
      ],
    });
    expect(screen.getAllByTestId("action-group").map((group) => group.textContent)).toEqual(["a1,a2", "b1"]);
  });
});
