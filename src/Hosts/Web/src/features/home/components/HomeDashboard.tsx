"use client";

import { Card, Link } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLocalizedText } from "@/localization";

interface DemoModuleCard {
  title: string;
  description: string;
  href: string;
  metric: string;
  callToAction: string;
}

export function HomeDashboard() {
  const { displayProfile, isLoading } = useAuthSession();
  const { t } = useLocalizedText();
  const demoModules: DemoModuleCard[] = [
    {
      title: t("home.modules.knowledge.title"),
      description: t("home.modules.knowledge.description"),
      href: Routes.Knowledge.Index,
      metric: t("home.modules.knowledge.metric"),
      callToAction: t("home.modules.knowledge.callToAction"),
    },
  ];
  const displayName = displayProfile.givenName ?? displayProfile.displayLabel;

  return (
    <main className="min-h-screen px-4 py-8 md:px-8 lg:px-12">
      <div className="mx-auto flex max-w-7xl flex-col gap-8">
        <section className="flex flex-col gap-6 p-6 md:p-10">
          <div className="flex flex-col gap-6 lg:grid lg:grid-cols-[1.2fr_0.8fr] lg:items-end">
            <div className="flex flex-col gap-6">
              <div className="flex flex-col gap-4">
                <p className="text-sm font-semibold uppercase tracking-[0.35em] text-muted">
                  {t("home.eyebrow")}
                </p>
                <h1 className="max-w-4xl text-4xl font-black tracking-tight text-foreground md:text-6xl">
                  {isLoading ? t("home.welcomeBack") : t("home.welcomeBackNamed", { name: displayName })}
                </h1>
                <p className="max-w-2xl text-lg leading-8 text-muted md:text-xl">
                  {t("home.intro")}
                </p>
              </div>

              <div className="flex flex-col gap-3 sm:flex-row">
                <Link href={Routes.Knowledge.Index} className="inline-flex min-h-12 items-center justify-center rounded-md bg-accent px-6 font-semibold text-accent-foreground transition hover:opacity-90">
                  {t("home.startKnowledge")}
                </Link>
                <Link href={Routes.Account.Index} className="inline-flex min-h-12 items-center justify-center rounded-md border border-separator px-6 font-semibold text-foreground transition hover:bg-surface-secondary">
                  {t("home.reviewAccount")}
                </Link>
              </div>
            </div>

            <Card>
              <Card.Header className="flex flex-col items-start gap-2">
                <p className="text-sm font-semibold uppercase tracking-[0.25em] text-muted">{t("home.nextMove")}</p>
                <h2 className="text-2xl font-bold">{t("home.exploreModuleDemo")}</h2>
              </Card.Header>
              <Card.Content className="text-sm leading-6 text-muted">
                {t("home.firstDemoDescription")}
              </Card.Content>
            </Card>
          </div>
        </section>

        <section aria-labelledby="module-demos" className="flex flex-col gap-4">
          <div className="flex flex-col justify-between gap-2 md:flex-row md:items-end">
            <div>
              <p className="text-sm font-semibold uppercase tracking-[0.3em] text-muted">{t("home.availableModules")}</p>
              <h2 id="module-demos" className="text-3xl font-bold text-foreground">{t("home.chooseNext")}</h2>
            </div>
            <p className="max-w-xl text-sm leading-6 text-muted">
              {t("home.cardsSourceNote")}
            </p>
          </div>

          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
            {demoModules.map((module) => (
              <Link key={module.href} href={module.href} className="block">
                <Card className="h-full">
                  <Card.Header className="flex flex-col items-start gap-3">
                    <span className="text-xs font-semibold uppercase tracking-[0.2em] text-muted">
                      {module.metric}
                    </span>
                    <h3 className="text-2xl font-bold text-foreground">{module.title}</h3>
                  </Card.Header>
                  <Card.Content className="flex flex-1 flex-col justify-between gap-6 text-sm leading-6 text-muted">
                    <p>{module.description}</p>
                    <span className="inline-flex items-center gap-2 font-semibold">
                      {module.callToAction}
                      <span aria-hidden="true">→</span>
                    </span>
                  </Card.Content>
                </Card>
              </Link>
            ))}
          </div>
        </section>
      </div>
    </main>
  );
}
