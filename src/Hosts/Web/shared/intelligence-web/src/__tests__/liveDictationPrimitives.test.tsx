import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/web-localization";
import { PromptInput } from "@heroui-pro/react";
import { intelligenceMessageCatalogs } from "../localization/catalogs";
import { LiveDictationLanguageSelect } from "../components/LiveDictationLanguageSelect";
import { LiveDictationPromptAction } from "../components/LiveDictationPromptAction";
import { LiveDictationStatus } from "../components/LiveDictationStatus";
import type { LiveDictationController } from "../hooks/useLiveDictation";

afterEach(cleanup);

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={intelligenceMessageCatalogs.en}>
      {children}
    </LocalizationProvider>
  );
}

function controller(
  overrides: Partial<LiveDictationController> = {},
): LiveDictationController {
  return {
    available: true,
    capabilitiesLoading: false,
    failureCode: null,
    isActive: false,
    language: "en-US",
    languages: ["en-US", "fa-IR"],
    setLanguage: vi.fn(),
    start: vi.fn(async () => undefined),
    status: "idle",
    stop: vi.fn(async () => undefined),
    ...overrides,
  };
}

function ActionHarness({ value }: { value: LiveDictationController }) {
  return (
    <PromptInput value="" onValueChange={() => undefined}>
      <LiveDictationPromptAction controller={value} />
    </PromptInput>
  );
}

describe("live dictation primitives", () => {
  it("exposes start and stop as pressed accessible prompt actions", () => {
    const idle = controller();
    const { rerender } = render(<ActionHarness value={idle} />, { wrapper: Wrapper });

    const start = screen.getByRole("button", { name: "Start voice dictation" });
    fireEvent.keyDown(start, { key: "Enter" });
    fireEvent.keyUp(start, { key: "Enter" });
    expect(idle.start).toHaveBeenCalledOnce();
    expect(start).toHaveAttribute("aria-pressed", "false");

    const recording = controller({ isActive: true, status: "recording" });
    rerender(<ActionHarness value={recording} />);
    const stop = screen.getByRole("button", { name: "Stop voice dictation" });
    expect(stop).toHaveAttribute("aria-pressed", "true");
    fireEvent.click(stop);
    expect(recording.stop).toHaveBeenCalledOnce();
  });

  it("disables unavailable actions and language changes during active sessions", () => {
    const unavailable = controller({
      available: false,
      failureCode: "CapabilityUnavailable",
      status: "unavailable",
    });
    const { rerender } = render(<ActionHarness value={unavailable} />, { wrapper: Wrapper });
    expect(screen.getByRole("button", { name: "Voice dictation unavailable" })).toBeDisabled();

    const active = controller({ isActive: true, status: "recording" });
    rerender(<LiveDictationLanguageSelect controller={active} />);
    expect(screen.getByRole("combobox", { name: "Dictation language" })).toBeDisabled();
  });

  it("selects supported languages and falls back to server language tags", () => {
    const setLanguage = vi.fn();
    const value = controller({ languages: ["en-US", "de-DE"], setLanguage });
    render(<LiveDictationLanguageSelect controller={value} />, { wrapper: Wrapper });

    expect(screen.getByRole("option", { name: "English" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "de-DE" })).toBeInTheDocument();
    fireEvent.change(screen.getByRole("combobox", { name: "Dictation language" }), {
      target: { value: "de-DE" },
    });
    expect(setLanguage).toHaveBeenCalledWith("de-DE");
  });

  it("announces lifecycle progress as status and stable failures as alerts", () => {
    const { rerender } = render(
      <LiveDictationStatus controller={controller({ isActive: true, status: "connecting" })} />,
      { wrapper: Wrapper },
    );
    expect(screen.getByRole("status")).toHaveTextContent("Connecting voice dictation");

    rerender(
      <LiveDictationStatus
        controller={controller({ failureCode: "PermissionDenied", status: "failed" })}
      />,
    );
    expect(screen.getByRole("alert")).toHaveTextContent("Microphone access was not granted.");
  });
});
