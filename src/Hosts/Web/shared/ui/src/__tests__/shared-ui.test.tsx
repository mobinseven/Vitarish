import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import {
  AuthMethodList,
  ChatBody,
  ChatCallout,
  ChatConversation,
  ChatEmptyState,
  ChatLoadingMessage,
  ChatMessage,
  ChatPromptInput,
  ChatSource,
  ChatSourceList,
  DashboardFrame,
  EmptyStateSurface,
  ErrorStateSurface,
  HeaderIcon,
  LoadingStateSurface,
  LoginPanel,
  OptionSelector,
  SettingsBackLink,
  SettingsSection,
  SettingsSurface,
  StatusAlert,
} from "../index";

describe("@vitarish/ui presentation components", () => {
  it("renders shared auth method links from caller-provided labels and hrefs", () => {
    render(
      <AuthMethodList
        methods={[
          { id: "password", href: "/login/password", label: "Password" },
        ]}
      />,
    );

    expect(screen.getByRole("link", { name: "Password" })).toHaveAttribute(
      "href",
      "/login/password",
    );
  });

  it("renders the login panel without app providers", () => {
    render(
      <LoginPanel
        title="Sign in"
        subtitle="Choose a method"
        backHref="/login"
        backLabel="Other methods"
      >
        <button type="button">Continue</button>
      </LoginPanel>,
    );

    expect(
      screen.getByRole("heading", { name: "Sign in" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Other methods" })).toHaveAttribute(
      "href",
      "/login",
    );
  });

  it("renders state surfaces from caller-provided messages", () => {
    render(
      <>
        <LoadingStateSurface label="Loading records" />
        <EmptyStateSurface
          title="No records"
          description="Create the first record."
        />
        <ErrorStateSurface title="Could not load" description="Try again." />
      </>,
    );

    expect(screen.getByText("Loading records")).toBeInTheDocument();
    expect(screen.getByText("No records")).toBeInTheDocument();
    expect(screen.getByText("Could not load")).toBeInTheDocument();
  });

  it("renders status alerts from caller-provided text", () => {
    render(<StatusAlert type="success" message="Saved" />);

    expect(screen.getByText("Saved")).toBeInTheDocument();
  });

  it("renders settings layout primitives and option labels", () => {
    const select = vi.fn();

    render(
      <SettingsSurface>
        <SettingsSection title="Preferences">
          <OptionSelector
            title="Language"
            selected="en"
            onSelect={select}
            options={[
              { value: "en", label: "English" },
              { value: "fa", label: "Persian" },
            ]}
          />
          <SettingsBackLink href="/account" label="Back to settings" />
        </SettingsSection>
      </SettingsSurface>,
    );

    expect(
      screen.getByRole("heading", { name: "Preferences" }),
    ).toBeInTheDocument();
    expect(screen.getByText("English")).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "Back to settings" }),
    ).toHaveAttribute("href", "/account");
  });

  it("renders chat empty state without feature data", () => {
    render(
      <ChatEmptyState
        title="Ask a question"
        description="Answers appear here."
      />,
    );

    expect(screen.getByText("Ask a question")).toBeInTheDocument();
    expect(screen.getByText("Answers appear here.")).toBeInTheDocument();
  });

  it("renders chat conversation primitives from caller-provided labels", () => {
    render(
      <ChatConversation>
        <ChatMessage kind="question">What is covered?</ChatMessage>
        <ChatMessage
          avatarAlt="Assistant"
          avatarFallback="AI"
          kind="answer"
          status="Answered"
          title="Guidance"
          tone="success"
        >
          <ChatBody>Coverage is available.</ChatBody>
          <ChatCallout tone="warning">
            Check the source before acting.
          </ChatCallout>
          <ChatSourceList title="Sources">
            <ChatSource
              title="Guide"
              meta="Score 1.00"
              body="Relevant excerpt"
            />
          </ChatSourceList>
        </ChatMessage>
        <ChatLoadingMessage
          avatarAlt="Assistant"
          avatarFallback="AI"
          loadingLabel="Thinking"
          message="Searching context"
          title="Working"
        />
      </ChatConversation>,
    );

    expect(screen.getByText("What is covered?")).toBeInTheDocument();
    expect(screen.getByText("Guidance")).toBeInTheDocument();
    expect(screen.getByText("Answered")).toBeInTheDocument();
    expect(screen.getByText("Coverage is available.")).toBeInTheDocument();
    expect(
      screen.getByText("Check the source before acting."),
    ).toBeInTheDocument();
    expect(screen.getByText("Sources")).toBeInTheDocument();
    expect(screen.getByText("Working")).toBeInTheDocument();
  });

  it("renders chat prompt input from caller-provided accessibility text", () => {
    render(
      <ChatPromptInput
        footer="Answers may cite sources."
        helperIcon={<span aria-hidden="true">#</span>}
        helperText="All sources"
        label="Question"
        placeholder="Ask a question"
        submitLabel="Send question"
        toolbarStart={<span>Attach context</span>}
        value="Can I reuse this?"
        onChange={vi.fn()}
        onSubmit={vi.fn()}
      />,
    );

    expect(screen.getByLabelText("Question")).toHaveValue("Can I reuse this?");
    expect(screen.getByText("Attach context")).toBeInTheDocument();
    expect(screen.getByText("All sources")).toBeInTheDocument();
    expect(screen.getByText("Answers may cite sources.")).toBeInTheDocument();
  });

  it("does not submit the prompt through the keyboard while sending is disabled", () => {
    const onSubmit = vi.fn();
    render(
      <ChatPromptInput
        isSendDisabled
        label="Question"
        placeholder="Ask a question"
        submitLabel="Send question"
        value="Do not send this"
        onChange={vi.fn()}
        onSubmit={onSubmit}
      />,
    );

    fireEvent.keyDown(screen.getAllByLabelText("Question").at(-1)!, { key: "Enter", code: "Enter" });

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("owns page scrolling in its labeled, focusable main content region", () => {
    const { container } = render(
      <DashboardFrame
        contentMode="page"
        currentPath="/patients"
        identity={{
          accountHref: "/account",
          accountLabel: "Account",
          initials: "TU",
          label: "Test User",
          subtitle: "Account",
        }}
        navigate={vi.fn()}
        mobileNavigation="sidebar"
        navigationGroups={[]}
        navigationLabel="Dashboard navigation"
        skipLinkLabel="Skip to main content"
        title="Patients"
      >
        <div>Dashboard content</div>
      </DashboardFrame>,
    );

    expect(container.querySelector("[data-app-layout]")).toHaveAttribute(
      "data-scroll-mode",
      "content",
    );
    expect(container.querySelector("[data-app-layout]")).toHaveAttribute(
      "data-content-mode",
      "page",
    );
    expect(
      screen.getByRole("main", { name: "Scrollable main content" }),
    ).toHaveAttribute("tabindex", "0");
    expect(
      screen.getByRole("link", { name: "Skip to main content" }),
    ).toHaveAttribute("href", "#vitarish-dashboard-main");
  });

  it("paints header icons neutral unless a status tone is given", () => {
    const { container } = render(
      <>
        <HeaderIcon icon="users" level="page" />
        <HeaderIcon icon="triangle-exclamation" tone="warning" />
      </>,
    );

    const [neutralIcon, statusIcon] = container.querySelectorAll("span[aria-hidden]");
    expect(neutralIcon).toHaveClass("bg-surface", "text-foreground");
    expect(neutralIcon).not.toHaveClass("bg-accent");
    expect(statusIcon).toHaveClass("bg-warning-soft", "text-warning");
  });
});
