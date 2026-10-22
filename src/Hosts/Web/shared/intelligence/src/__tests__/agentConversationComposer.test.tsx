import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AgentConversationComposer } from "../components/AgentConversationComposer";

const useLiveDictation = vi.fn();
const promptInput = vi.fn();

vi.mock("../hooks/useLiveDictation", () => ({
  appendConfirmedTranscript: (current: string, segment: string) => {
    const confirmed = segment.trim();
    return current ? `${current} ${confirmed}` : confirmed;
  },
  useLiveDictation: (...args: unknown[]) => useLiveDictation(...args),
}));
vi.mock("../components/LiveDictation", () => ({
  LiveDictation: () => <span data-testid="live-dictation" />,
}));
vi.mock("@vitarish/ui", () => ({
  ChatPromptInput: (props: any) => {
    promptInput(props);
    return (
      <form
        data-layout="compact"
        onSubmit={(event) => {
          event.preventDefault();
          props.onSubmit(props.value);
        }}
      >
        <textarea
          aria-label={props.label}
          value={props.value}
          onChange={(event) => props.onChange(event.target.value)}
        />
        <button disabled={props.isSendDisabled} type="submit">
          {props.submitLabel}
        </button>
        {props.actions}
      </form>
    );
  },
}));

afterEach(() => {
  vi.clearAllMocks();
});

describe("AgentConversationComposer", () => {
  it("uses the compact controlled composer and guards send while recording", () => {
    useLiveDictation.mockReturnValue({ isActive: true });
    const onChange = vi.fn();
    const onSubmit = vi.fn();

    const { container } = render(
      <AgentConversationComposer
        direction="rtl"
        label="Ask the agent"
        onChange={onChange}
        onSubmit={onSubmit}
        placeholder="Ask"
        preferredLanguage="fa-IR"
        sessionKey="patient-1"
        submitLabel="Send"
        value="Draft"
      />,
    );

    expect(container.querySelector("form")).toHaveAttribute(
      "data-layout",
      "compact",
    );
    expect(screen.getByRole("textbox", { name: "Ask the agent" })).toHaveValue(
      "Draft",
    );
    expect(screen.getByRole("button", { name: "Send" })).toBeDisabled();
    expect(screen.getByTestId("live-dictation")).toBeInTheDocument();
    expect(promptInput).toHaveBeenCalledWith(
      expect.objectContaining({ direction: "rtl", isSendDisabled: true }),
    );
  });

  it("appends confirmed transcript to the caller-owned draft", () => {
    const onChange = vi.fn();
    useLiveDictation.mockImplementation((options: any) => {
      options.onSegment(" confirmed speech ");
      return { isActive: false };
    });

    render(
      <AgentConversationComposer
        label="Ask"
        onChange={onChange}
        onSubmit={vi.fn()}
        placeholder="Ask"
        preferredLanguage="en-US"
        sessionKey="workspace-1"
        submitLabel="Send"
        value="Existing draft"
      />,
    );

    expect(onChange).toHaveBeenCalledWith("Existing draft confirmed speech");
    expect(useLiveDictation).toHaveBeenCalledWith(
      expect.objectContaining({ enabled: true, sessionKey: "workspace-1" }),
    );
  });

  it("offers no dictation and checks no capability where dictation is not offered", () => {
    useLiveDictation.mockReturnValue({ isActive: false });

    const { container } = render(
      <AgentConversationComposer
        dictation={false}
        label="Ask"
        onChange={vi.fn()}
        onSubmit={vi.fn()}
        placeholder="Ask"
        preferredLanguage="fa-IR"
        sessionKey="public-page"
        submitLabel="Send"
        value=""
      />,
    );

    expect(container.querySelector("[data-testid=\"live-dictation\"]")).toBeNull();
    expect(useLiveDictation).toHaveBeenCalledWith(expect.objectContaining({ enabled: false }));
  });

  it("disables dictation lifecycle when its context is disabled", () => {
    useLiveDictation.mockReturnValue({ isActive: false });

    render(
      <AgentConversationComposer
        enabled={false}
        isPending
        label="Ask"
        onChange={vi.fn()}
        onSubmit={vi.fn()}
        placeholder="Ask"
        preferredLanguage="en-US"
        sessionKey="disabled-context"
        submitLabel="Send"
        value=""
      />,
    );

    expect(useLiveDictation).toHaveBeenCalledWith(
      expect.objectContaining({
        enabled: false,
        sessionKey: "disabled-context",
      }),
    );
  });
});
