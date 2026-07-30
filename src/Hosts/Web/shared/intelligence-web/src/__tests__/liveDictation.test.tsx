import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/web-localization";
import { PromptInput } from "@heroui-pro/react";
import { intelligenceMessageCatalogs } from "../localization/catalogs";
import { LiveDictation } from "../components/LiveDictation";
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

function Harness({ value }: { value: LiveDictationController }) {
  return (
    <PromptInput value="" onValueChange={() => undefined}>
      <LiveDictation controller={value} />
    </PromptInput>
  );
}

function getDictationButton(name: string) {
  const button = screen
    .getAllByRole("button", { name })
    .find((element) => element.tagName === "BUTTON");
  if (!button) throw new Error(`Expected dictation button: ${name}`);
  return button;
}

describe("LiveDictation", () => {
  it("gates the action while availability is loading", () => {
    render(<Harness value={controller({ capabilitiesLoading: true })} />, {
      wrapper: Wrapper,
    });

    expect(
      getDictationButton("Start voice dictation"),
    ).toHaveAttribute("aria-disabled", "true");
    expect(screen.getByRole("status")).toHaveTextContent(
      "Checking voice dictation availability",
    );
  });

  it("morphs into language choices and starts with the selected language", () => {
    const value = controller();
    render(<Harness value={value} />, { wrapper: Wrapper });

    fireEvent.click(
      getDictationButton("Start voice dictation"),
    );
    const languages = screen.getByRole("group", { name: "Dictation language" });
    expect(languages).toHaveAttribute("aria-hidden", "false");

    fireEvent.click(screen.getByRole("button", { name: "Persian" }));
    expect(value.setLanguage).toHaveBeenCalledWith("fa-IR");
    expect(value.start).toHaveBeenCalledWith("fa-IR");
    expect(languages).toHaveAttribute("aria-hidden", "true");
  });

  it("renders an active red pulsing stop action and stops on press", () => {
    const value = controller({ isActive: true, status: "recording" });
    render(<Harness value={value} />, { wrapper: Wrapper });

    const stop = screen.getByRole("button", { name: "Stop voice dictation" });
    expect(stop).toHaveAttribute("aria-pressed", "true");
    expect(stop).toHaveClass("text-danger", "animate-pulse");
    expect(screen.getByRole("status")).toHaveTextContent(
      "Recording. Select stop when you are finished.",
    );
    fireEvent.click(stop);
    expect(value.stop).toHaveBeenCalledOnce();
  });

  it("disables unavailable dictation and announces stable failures", () => {
    render(
      <Harness
        value={controller({
          available: false,
          failureCode: "CapabilityUnavailable",
          languages: [],
          status: "unavailable",
        })}
      />,
      { wrapper: Wrapper },
    );

    expect(
      getDictationButton("Voice dictation unavailable"),
    ).toBeDisabled();
    expect(screen.getByRole("status")).toHaveTextContent(
      "Voice dictation is currently unavailable.",
    );
  });

  it("renders session failures as visible alerts", () => {
    render(
      <Harness
        value={controller({
          failureCode: "PermissionDenied",
          status: "failed",
        })}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.getByRole("alert")).toHaveTextContent(
      "Microphone access was not granted.",
    );
  });
});
