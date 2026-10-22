import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { InteractiveOptionPicker } from "../components/InteractiveOptionPicker";
import { toInteractiveReply } from "../interaction/interactiveReply";

afterEach(cleanup);

const options = [
  { key: "dental", label: "Dental" },
  { key: "physio", label: "Physiotherapy" },
  { key: "psych", label: "Psychology" },
];

describe("InteractiveOptionPicker", () => {
  it("answers a single-choice question on the first tap", () => {
    const onAnswer = vi.fn();
    render(<InteractiveOptionPicker question={{ prompt: "What kind?", options, maxSelections: 1 }} onAnswer={onAnswer} />);

    fireEvent.click(screen.getByRole("radio", { name: "Physiotherapy" }));

    expect(onAnswer).toHaveBeenCalledWith(["physio"]);
  });

  it("shows an answered question as its chosen options and offers nothing more to answer", () => {
    render(
      <InteractiveOptionPicker answered={["dental"]} question={{ prompt: "What kind?", options, maxSelections: 1 }} onAnswer={vi.fn()} />,
    );

    expect(screen.getByRole("list", { name: "What kind?" })).toHaveTextContent("Dental");
    expect(screen.queryByRole("radio")).toBeNull();
    expect(screen.queryByText("Psychology")).toBeNull();
  });

  it("collects a multiple-choice answer up to its limit and sends it once", () => {
    const onAnswer = vi.fn();
    render(<InteractiveOptionPicker question={{ prompt: "What slows you?", options, maxSelections: 2 }} onAnswer={onAnswer} />);
    const send = screen.getByRole("button", { name: /intelligence.interactive.submit/ });
    expect(send).toBeDisabled();

    fireEvent.click(screen.getByRole("checkbox", { name: "Dental" }));
    fireEvent.click(screen.getByRole("checkbox", { name: "Psychology" }));
    expect(screen.getByRole("checkbox", { name: "Physiotherapy" })).toBeDisabled();
    fireEvent.click(send);

    expect(onAnswer).toHaveBeenCalledWith(["dental", "psych"]);
  });
});

describe("toInteractiveReply", () => {
  it("reads integers sent as strings and keeps the selection limit within the options", () => {
    const reply = toInteractiveReply({ text: "Pick", question: { prompt: "Which?", options, maxSelections: "9" }, references: ["a"], nextAction: null });

    expect(reply.question?.maxSelections).toBe(3);
    expect(reply.references).toEqual(["a"]);
  });

  it("treats a question without options as no question", () => {
    expect(toInteractiveReply({ text: "Hi", question: { prompt: "?", options: [], maxSelections: 1 } }).question).toBeNull();
  });
});
