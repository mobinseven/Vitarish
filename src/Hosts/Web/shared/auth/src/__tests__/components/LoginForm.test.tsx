import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode } from "react";
import { http, HttpResponse } from "msw";
import { server } from "../server";
import { LoginForm } from "../../components/LoginForm";
import { LocalizationProvider } from "@vitarish/localization";

vi.mock("@vitarish/ui", () => ({
  StatusAlert: ({ message }: { message: string }) => <div>{message}</div>,
}));

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

function wrapper({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <LocalizationProvider
        locale="en"
        messages={{
          auth: {
            login: {
              usernameOrEmailLabel: "Username or email",
              usernameOrEmailPlaceholder: "Enter",
              passwordLabel: "Password",
              passwordPlaceholder: "Enter",
              signInAction: "Sign in",
            },
            accountStatus: { disabled: "This account is disabled. Contact a platform administrator." },
            recovery: { forgotPasswordAction: "Forgot password?" },
          },
        }}
      >
        {children}
      </LocalizationProvider>
    </QueryClientProvider>
  );
}

describe("LoginForm", () => {
  afterEach(() => cleanup());
  beforeEach(() => {
    cleanup();
    queryClient.clear();
  });

  it("should render username and password fields", () => {
    render(<LoginForm forgotPasswordHref="/login/forgot-password" />, { wrapper });

    expect(screen.getByLabelText(/username or email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /sign in/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /forgot password/i })).toHaveAttribute(
      "href",
      "/login/forgot-password",
    );
  });

  it("should render with correct input types", () => {
    render(<LoginForm />, { wrapper });

    const passwordInput = screen.getByLabelText(/password/i);
    expect(passwordInput).toHaveAttribute("type", "password");
  });

  it("uses a POST fallback so credentials cannot appear in the URL before hydration", () => {
    render(<LoginForm />, { wrapper });

    expect(screen.getByRole("button", { name: /sign in/i }).closest("form")).toHaveAttribute("method", "post");
  });

  it("shows the localized disabled-account message after a verified sign-in", async () => {
    server.use(
      http.post(
        /\/api\/modules\/authentication\/login\/password(?:\?|$)/,
        () =>
          HttpResponse.json(
            {
              status: 403,
              title: "AccountDisabled",
              detail: "The account is disabled.",
              code: "account_disabled",
            },
            { status: 403 },
          ),
      ),
    );
    render(<LoginForm />, { wrapper });

    fireEvent.change(screen.getByLabelText(/username or email/i), { target: { value: "disabled@test.com" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText("This account is disabled. Contact a platform administrator.")).toBeInTheDocument();
    });
  });
});
