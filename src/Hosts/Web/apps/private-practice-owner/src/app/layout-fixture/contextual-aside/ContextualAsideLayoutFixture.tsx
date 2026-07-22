"use client";

import { DashboardFrame } from "@vitarish/web-ui";
import { ContainedPage } from "@vitarish/web-ui/layout";
import { useLocalizedText } from "@/localization";

interface ContextualAsideLayoutFixtureProps {
  defaultAsideOpen: boolean;
}

export function ContextualAsideLayoutFixture({
  defaultAsideOpen,
}: ContextualAsideLayoutFixtureProps) {
  const { t } = useLocalizedText();
  const aside = <ContextualAsideContent />;
  const navigationGroups = [
    {
      label: t("layoutFixture.navigationGroupLabel"),
      items: [
        {
          href: "/layout-fixture/contextual-aside",
          icon: "table-columns",
          label: t("layoutFixture.navigationItemLabel"),
        },
      ],
    },
  ];

  return (
    <DashboardFrame
      aside={aside}
      asideMobile="sheet"
      asideToggleLabel={t("layoutFixture.asideToggleLabel")}
      contentMode="page"
      currentPath="/layout-fixture/contextual-aside"
      defaultAsideOpen={defaultAsideOpen}
      identity={{
        accountHref: "/account",
        accountLabel: t("layoutFixture.accountLabel"),
        initials: t("layoutFixture.identityInitials"),
        label: t("layoutFixture.identityLabel"),
        subtitle: t("layoutFixture.identitySubtitle"),
      }}
      mobileAside={aside}
      mobileNavigation="sidebar"
      navigate={() => undefined}
      navigationGroups={navigationGroups}
      navigationLabel={t("layoutFixture.navigationLabel")}
      skipLinkLabel={t("layoutFixture.skipLinkLabel")}
      title={<span className="font-semibold">{t("layoutFixture.title")}</span>}
    >
      <ContainedPage>
        <section aria-labelledby="layout-fixture-heading" className="flex flex-col gap-6">
          <div>
            <p className="text-sm font-semibold uppercase tracking-wider text-muted">
              {t("layoutFixture.eyebrow")}
            </p>
            <h1 id="layout-fixture-heading" className="text-3xl font-bold">
              {t("layoutFixture.heading")}
            </h1>
          </div>
          {Array.from({ length: 18 }, (_, index) => (
            <article
              key={index}
              className="rounded-xl border border-border bg-surface p-5"
            >
              <h2 className="font-semibold">
                {t("layoutFixture.probeTitle", { number: index + 1 })}
              </h2>
              <p className="text-sm text-muted">
                {t("layoutFixture.probeDescription")}
              </p>
            </article>
          ))}
        </section>
      </ContainedPage>
    </DashboardFrame>
  );
}

function ContextualAsideContent() {
  const { t } = useLocalizedText();

  return (
    <div
      className="flex min-h-0 flex-col gap-4 p-5"
      data-layout-test="contextual-aside"
    >
      <h2 className="text-lg font-semibold">{t("layoutFixture.asideTitle")}</h2>
      <p className="text-sm text-muted">
        {t("layoutFixture.asideDescription")}
      </p>
    </div>
  );
}
