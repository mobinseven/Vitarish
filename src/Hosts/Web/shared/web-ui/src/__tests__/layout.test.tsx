import { createRef } from "react";
import { render, screen } from "@testing-library/react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";
import { I18nProvider } from "@vitarish/web-localization";
import {
  CenteredRegion,
  ContainedPage,
  DashboardFrame,
  ImmersiveRegion,
  PageContainer,
  PublicDocumentShell,
  StandaloneCenteredPage,
  StandaloneDocumentPage,
  StandaloneImmersivePage,
  pageWidths,
} from "../index";

describe("shared layout policy components", () => {
  it("composes public chrome around exactly one standalone main landmark", () => {
    const ref = createRef<HTMLDivElement>();

    const { container } = render(
      <PublicDocumentShell
        ref={ref}
        className="caller-shell"
        data-testid="public-shell"
        header={<nav aria-label="Public navigation">Navigation</nav>}
        footer={<p>Footer content</p>}
      >
        <StandaloneDocumentPage id="main-content">
          <PageContainer width="reading">Article</PageContainer>
        </StandaloneDocumentPage>
      </PublicDocumentShell>,
    );

    expect(ref.current).toBe(screen.getByTestId("public-shell"));
    expect(ref.current).toHaveClass(
      "vitarish-public-document-shell",
      "caller-shell",
    );
    expect(container.querySelectorAll("main")).toHaveLength(1);
    expect(screen.getByRole("main")).toHaveAttribute(
      "data-vitarish-layout",
      "standalone-document-page",
    );
    expect(
      screen.getByRole("navigation", { name: "Public navigation" }),
    ).toBeInTheDocument();
    expect(container.querySelector("footer")).toHaveTextContent(
      "Footer content",
    );
  });

  it("exposes every typed page width without changing the element contract", () => {
    render(
      <>
        {pageWidths.map((width) => (
          <PageContainer
            key={width}
            data-testid={`width-${width}`}
            width={width}
          >
            {width}
          </PageContainer>
        ))}
      </>,
    );

    for (const width of pageWidths) {
      expect(screen.getByTestId(`width-${width}`)).toHaveAttribute(
        "data-vitarish-page-width",
        width,
      );
    }
  });

  it("keeps framed regions landmark-neutral and forwards HTML props and refs", () => {
    const ref = createRef<HTMLDivElement>();
    const { container } = render(
      <ContainedPage ref={ref} aria-label="Settings" width="fluid">
        <CenteredRegion data-testid="gate">Gate</CenteredRegion>
      </ContainedPage>,
    );

    expect(ref.current).toHaveAttribute("aria-label", "Settings");
    expect(ref.current).toHaveAttribute("data-vitarish-page-width", "fluid");
    expect(
      container.querySelector("[data-vitarish-reveal-header-host]"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("gate")).toHaveAttribute(
      "data-vitarish-layout",
      "centered-region",
    );
    expect(container.querySelector("main")).toBeNull();
  });

  it("distinguishes document, centered, and immersive standalone main policies", () => {
    const { container } = render(
      <>
        <StandaloneDocumentPage>Document</StandaloneDocumentPage>
        <StandaloneCenteredPage width="compact">
          Centered
        </StandaloneCenteredPage>
        <StandaloneImmersivePage>
          <ImmersiveRegion data-testid="workspace-region">
            Workspace
          </ImmersiveRegion>
        </StandaloneImmersivePage>
      </>,
    );

    const mains = container.querySelectorAll("main");
    expect(mains).toHaveLength(3);
    expect(mains[0]).toHaveAttribute("data-vitarish-scroll-owner", "document");
    expect(mains[1]).toHaveAttribute(
      "data-vitarish-layout",
      "standalone-centered-page",
    );
    expect(mains[2]).toHaveAttribute("data-vitarish-scroll-owner", "workspace");
    expect(screen.getByTestId("workspace-region")).toHaveAttribute(
      "data-vitarish-layout",
      "immersive-region",
    );
    expect(screen.getByTestId("workspace-region")).toHaveAttribute(
      "data-vitarish-reveal-header-region",
      "workspace",
    );
  });

  it("renders non-interactive primitives on the server without browser APIs", () => {
    expect(() =>
      renderToStaticMarkup(
        <PublicDocumentShell>
          <StandaloneDocumentPage>
            <ContainedPage width="content">Server content</ContainedPage>
          </StandaloneDocumentPage>
        </PublicDocumentShell>,
      ),
    ).not.toThrow();
  });

  it("keeps HeroUI main ownership while selecting workspace scroll policy and native aside", () => {
    const { container } = render(
      <DashboardFrame
        aside={<div>Context panel</div>}
        asideToggleLabel="Toggle context panel"
        contentMode="workspace"
        currentPath="/workspace"
        identity={{
          accountHref: "/account",
          accountLabel: "Account",
          initials: "TU",
          label: "Test User",
          subtitle: "Account",
        }}
        mobileNavigation="none"
        navigate={vi.fn()}
        navigationGroups={[]}
        navigationLabel="Dashboard navigation"
        skipLinkLabel="Skip to workspace"
        title="Workspace"
      >
        <ImmersiveRegion>Workspace content</ImmersiveRegion>
      </DashboardFrame>,
    );

    expect(container.querySelectorAll("main")).toHaveLength(1);
    expect(container.querySelector("[data-app-layout]")).toHaveAttribute(
      "data-content-mode",
      "workspace",
    );
    expect(
      container.querySelector("[data-slot='app-layout-main']"),
    ).toHaveAttribute("data-scroll-mode", "page");
    expect(
      container.querySelector("[data-slot='app-layout-header']"),
    ).toBeNull();
    expect(
      container.querySelector("#vitarish-dashboard-main > .navbar"),
    ).toBeInTheDocument();
    expect(container.querySelector("#vitarish-dashboard-main")).toHaveAttribute(
      "data-vitarish-scroll-owner",
      "workspace",
    );
    expect(
      container.querySelector(
        "#vitarish-dashboard-main .vitarish-reveal-header",
      ),
    ).toBeInTheDocument();
    expect(
      container.querySelector(
        "#vitarish-dashboard-main .vitarish-reveal-header-inset",
      ),
    ).toBeInTheDocument();
    expect(
      container.querySelector(
        "#vitarish-dashboard-main .vitarish-reveal-header [data-slot='navbar-header']",
      ),
    ).toHaveTextContent("Workspace");
    expect(
      container.querySelector("[data-slot='app-layout-menu-toggle']"),
    ).toBeNull();
    expect(
      screen.getAllByRole("button", { name: "Toggle context panel" }),
    ).toHaveLength(2);
    expect(container.querySelector("#vitarish-dashboard-main")).toHaveAttribute(
      "tabindex",
      "-1",
    );
  });

  it("keeps the desktop sidebar on the physical right in RTL layouts", () => {
    const { container } = render(
      <I18nProvider locale="fa">
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
          mobileNavigation="sidebar"
          navigate={vi.fn()}
          navigationGroups={[]}
          navigationLabel="Dashboard navigation"
          skipLinkLabel="Skip to content"
          title="Patients"
        >
          Content
        </DashboardFrame>
      </I18nProvider>,
    );

    expect(container.querySelector("[data-app-layout]")).toHaveAttribute(
      "data-direction",
      "rtl",
    );
    expect(container.querySelector("[data-slot='sidebar']")).toHaveAttribute(
      "data-side",
      "right",
    );
  });
});
