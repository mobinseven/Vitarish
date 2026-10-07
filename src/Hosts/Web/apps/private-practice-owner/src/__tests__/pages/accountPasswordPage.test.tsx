import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ApiError } from "@vitarish/api-client";
import AccountPasswordPage from "@/app/account/password/page";

const changePassword = vi.fn();

vi.mock("@vitarish/auth", () => ({
  useAuthSession: () => ({ isAuthenticated: true }),
}));

vi.mock("@vitarish/account", async () => {
  const { useState } = await import("react");
  return {
    useAccountPasswordChange: () => {
      const [error, setError] = useState<string | null>(null);
      const [success, setSuccess] = useState(false);
      return { isLoading: false, error, setError, success, setSuccess, changePassword };
    },
  };
});

vi.mock("@/components/account/AccountSurface", async () => {
  const { StandaloneDocumentPage } = await import("@vitarish/ui");
  return {
    AccountSurface: ({ children }: { children: React.ReactNode }) => (
      <StandaloneDocumentPage>{children}</StandaloneDocumentPage>
    ),
    AccountWidget: ({ children }: { children: React.ReactNode }) => <section>{children}</section>,
    AccountBackLink: () => <a href="/account">Back</a>,
  };
});

vi.mock("@/localization", () => ({
  localizeApiError: () => "Something went wrong",
  useLocalizedText: () => ({
    locale: "en",
    t: (key: string) => ({
      "account.password.policyRejected": "The new password does not meet the security policy. Choose a stronger password.",
      "account.password.mismatch": "Passwords do not match",
    }[key] ?? key),
  }),
}));

describe("AccountPasswordPage", () => {
  it("leaves the password policy to the server and shows its rejection", async () => {
    changePassword.mockRejectedValueOnce(new ApiError({ status: 400, title: "Invalid input", code: "invalid_input" }));
    const { container } = render(<AccountPasswordPage />);

    fireEvent.change(container.querySelector('input[name="currentPassword"]')!, { target: { value: "Current-Pass1!" } });
    fireEvent.change(container.querySelector('input[name="newPassword"]')!, { target: { value: "short" } });
    fireEvent.change(container.querySelector('input[name="confirmPassword"]')!, { target: { value: "short" } });
    fireEvent.submit(container.querySelector("form")!);

    await waitFor(() => expect(changePassword).toHaveBeenCalledWith("Current-Pass1!", "short"));
    expect(await screen.findByText("The new password does not meet the security policy. Choose a stronger password.")).toBeVisible();
  });
});
