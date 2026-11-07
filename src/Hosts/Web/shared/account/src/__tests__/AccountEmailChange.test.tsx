import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { I18nProvider } from "@vitarish/localization";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AccountEmailChange } from "../components/AccountEmailChange";

const mocks = vi.hoisted(() => ({
  userId: undefined as string | undefined,
  beginEmailChange: vi.fn(),
  setError: vi.fn(),
  state: {} as Record<string, unknown>,
}));

vi.mock("../hooks/useAccountEmailChange", () => ({
  useAccountEmailChange: (userId: string) => {
    mocks.userId = userId;
    return mocks.state;
  },
}));

vi.mock("@vitarish/ui", async () => {
  const { createElement } = await import("react");
  return {
    FaIcon: () => null,
    HeaderIcon: () => createElement("span", { "aria-hidden": true }),
    StatusAlert: ({ message }: { message: string }) => createElement("div", { role: "alert" }, message),
  };
});

const messages = {
  auth: { otp: { sendAction: "Send Code" } },
  account: { email: { title: "Change Email", newEmail: "New Email", currentValue: "Current: {value}", updateSucceeded: "Email updated" } },
  errors: { generic: "Something went wrong.", forbidden: "You do not have permission." },
};

function renderSection() {
  return render(
    <I18nProvider locale="en" messages={messages}>
      <AccountEmailChange userId="account-1" />
    </I18nProvider>,
  );
}

describe("account email change section", () => {
  afterEach(cleanup);

  beforeEach(() => {
    mocks.beginEmailChange.mockReset().mockResolvedValue(undefined);
    mocks.setError.mockReset();
    mocks.state = {
      profile: { email: "ali@example.test" },
      step: "email",
      email: "new@example.test",
      code: "",
      isLoading: false,
      error: null,
      success: false,
      setEmail: vi.fn(),
      setCode: vi.fn(),
      setError: mocks.setError,
      reset: vi.fn(),
      beginEmailChange: mocks.beginEmailChange,
      verifyEmailChange: vi.fn(),
    };
  });

  it("loads the profile of the given account and shows its current email", () => {
    renderSection();

    expect(mocks.userId).toBe("account-1");
    expect(screen.getByText("Current: ali@example.test")).not.toBeNull();
  });

  it("sends the typed address and shows a localized failure instead of leaking the raw error", async () => {
    mocks.beginEmailChange.mockRejectedValueOnce({ status: 403 });
    renderSection();

    fireEvent.click(screen.getByRole("button", { name: "Send Code" }));

    await waitFor(() => expect(mocks.beginEmailChange).toHaveBeenCalledWith("new@example.test"));
    expect(mocks.setError).toHaveBeenCalledWith("You do not have permission.");
  });
});
