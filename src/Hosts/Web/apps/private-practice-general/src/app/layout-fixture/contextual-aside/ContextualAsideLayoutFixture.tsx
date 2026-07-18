"use client";

import { DashboardFrame } from "@vitarish/web-ui";
import { ContainedPage } from "@vitarish/web-ui/layout";

interface ContextualAsideLayoutFixtureProps {
  defaultAsideOpen: boolean;
}

const navigationGroups = [
  {
    label: "Layout fixture",
    items: [
      {
        href: "/layout-fixture/contextual-aside",
        iconClassName: "fa-solid fa-table-columns",
        label: "Responsive layout",
      },
    ],
  },
] as const;

export function ContextualAsideLayoutFixture({
  defaultAsideOpen,
}: ContextualAsideLayoutFixtureProps) {
  const aside = <ContextualAsideContent />;

  return (
    <DashboardFrame
      aside={aside}
      asideMobile="sheet"
      asideToggleLabel="Toggle contextual panel"
      contentMode="page"
      currentPath="/layout-fixture/contextual-aside"
      defaultAsideOpen={defaultAsideOpen}
      identity={{
        accountHref: "/account",
        accountLabel: "Fixture account",
        initials: "LF",
        label: "Layout fixture",
        subtitle: "Responsive policy",
      }}
      mobileAside={aside}
      mobileNavigation="sidebar"
      navigate={() => undefined}
      navigationGroups={navigationGroups}
      navigationLabel="Fixture navigation"
      skipLinkLabel="Skip to fixture content"
      title={<span className="font-semibold">Layout policy fixture</span>}
    >
      <ContainedPage>
        <section aria-labelledby="layout-fixture-heading" className="flex flex-col gap-6">
          <div>
            <p className="text-sm font-semibold uppercase tracking-wider text-muted">
              Deterministic browser fixture
            </p>
            <h1 id="layout-fixture-heading" className="text-3xl font-bold">
              Contained dashboard content
            </h1>
          </div>
          {Array.from({ length: 18 }, (_, index) => (
            <article
              key={index}
              className="rounded-xl border border-border bg-surface p-5"
            >
              <h2 className="font-semibold">Layout probe {index + 1}</h2>
              <p className="text-sm text-muted">
                Stable content verifies that the HeroUI main region, rather than the document,
                owns page-axis scrolling.
              </p>
            </article>
          ))}
        </section>
      </ContainedPage>
    </DashboardFrame>
  );
}

function ContextualAsideContent() {
  return (
    <div
      className="flex min-h-0 flex-col gap-4 p-5"
      data-layout-test="contextual-aside"
    >
      <h2 className="text-lg font-semibold">Contextual panel</h2>
      <p className="text-sm text-muted">
        This content is rendered by the native AppLayout aside and mobile sheet.
      </p>
    </div>
  );
}
