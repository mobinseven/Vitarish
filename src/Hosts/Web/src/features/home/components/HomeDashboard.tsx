"use client";

import Link from "next/link";
import { ItemCard, Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { useLocalizedText } from "@/localization";

interface DemoModuleCard {
  title: string;
  description: string;
  href: string;
  metric: string;
  callToAction: string;
  accentClassName: string;
  icon: string;
}

interface WorkspaceMetric {
  value: string;
  label: string;
  className: string;
}

export function HomeDashboard() {
  const { displayProfile, isLoading } = useAuthSession();
  const { t, direction } = useLocalizedText();
  const isRtl = direction === "rtl";
  const displayName = displayProfile.givenName ?? displayProfile.displayLabel;

  const demoModules: DemoModuleCard[] = [
    {
      title: t("home.modules.knowledge.title"),
      description: t("home.modules.knowledge.description"),
      href: Routes.Knowledge.Index,
      metric: t("home.modules.knowledge.metric"),
      callToAction: t("home.modules.knowledge.callToAction"),
      accentClassName: "from-violet-500/20 via-fuchsia-500/10 to-transparent text-violet-600 dark:text-violet-300",
      icon: "K",
    },
    {
      title: t("home.modules.clinic.title"),
      description: t("home.modules.clinic.description"),
      href: Routes.Clinic.Index,
      metric: t("home.modules.clinic.metric"),
      callToAction: t("home.modules.clinic.callToAction"),
      accentClassName: "from-sky-500/20 via-cyan-500/10 to-transparent text-sky-600 dark:text-sky-300",
      icon: "C",
    },
    {
      title: t("account.title"),
      description: t("account.theme.description"),
      href: Routes.Account.Index,
      metric: t("account.navigation.settings"),
      callToAction: t("account.settings"),
      accentClassName: "from-emerald-500/20 via-teal-500/10 to-transparent text-emerald-600 dark:text-emerald-300",
      icon: "A",
    },
  ];

  const workspaceMetrics: WorkspaceMetric[] = [
    { value: String(demoModules.length), label: t("home.availableModules"), className: "text-violet-600 dark:text-violet-300" },
    { value: "1", label: t("home.nextMove"), className: "text-emerald-600 dark:text-emerald-300" },
    { value: "24/7", label: t("home.workspaceReady"), className: "text-amber-600 dark:text-amber-300" },
  ];

  return (
    <div className="relative isolate mx-auto flex max-w-7xl flex-col gap-6 overflow-hidden px-4 pb-10 pt-4 sm:px-6 lg:px-8">
      <div className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-80 rounded-[3rem] bg-[radial-gradient(circle_at_15%_20%,rgba(124,58,237,0.20),transparent_35%),radial-gradient(circle_at_85%_10%,rgba(20,184,166,0.18),transparent_34%),linear-gradient(135deg,rgba(255,255,255,0.85),rgba(255,255,255,0.36))] dark:bg-[radial-gradient(circle_at_15%_20%,rgba(124,58,237,0.24),transparent_35%),radial-gradient(circle_at_85%_10%,rgba(20,184,166,0.18),transparent_34%),linear-gradient(135deg,rgba(15,23,42,0.9),rgba(15,23,42,0.44))]" />

      <section className="grid gap-5 lg:grid-cols-[minmax(0,1.45fr)_minmax(320px,0.55fr)] lg:items-stretch">
        <div className="relative overflow-hidden rounded-[2rem] border border-separator bg-surface/85 p-6 shadow-2xl shadow-foreground/5 backdrop-blur md:p-10">
          <div className="pointer-events-none absolute -right-24 -top-24 h-72 w-72 rounded-full bg-violet-500/10 blur-3xl" />
          <div className="pointer-events-none absolute -bottom-32 left-12 h-72 w-72 rounded-full bg-emerald-500/10 blur-3xl" />

          <div className="relative flex min-h-[420px] flex-col justify-between gap-10">
            <div className="flex flex-col gap-6">
              <div className="inline-flex w-fit items-center gap-2 rounded-full border border-separator bg-surface-secondary/80 px-3 py-2 text-xs font-semibold uppercase tracking-[0.28em] text-muted">
                <span className="h-2 w-2 rounded-full bg-emerald-500 shadow-[0_0_0_6px_rgba(16,185,129,0.12)]" aria-hidden="true" />
                {t("home.eyebrow")}
              </div>

              <div className="flex flex-col gap-4">
                <h1 className="max-w-4xl text-4xl font-black tracking-tight text-foreground md:text-6xl xl:text-7xl">
                  {isLoading ? t("home.welcomeBack") : t("home.welcomeBackNamed", { name: displayName })}
                </h1>
                <p className="max-w-2xl text-base leading-8 text-muted md:text-xl">
                  {t("home.intro")}
                </p>
              </div>

              <div className="flex flex-col gap-3 sm:flex-row">
                <Link href={Routes.Knowledge.Index} className="inline-flex min-h-12 items-center justify-center rounded-2xl bg-accent px-6 font-semibold text-accent-foreground shadow-lg shadow-accent/20 transition hover:-translate-y-0.5 hover:opacity-90">
                  {t("home.startKnowledge")}
                </Link>
                <Link href={Routes.Account.Index} className="inline-flex min-h-12 items-center justify-center rounded-2xl border border-separator bg-surface/80 px-6 font-semibold text-foreground transition hover:-translate-y-0.5 hover:bg-surface-secondary">
                  {t("home.reviewAccount")}
                </Link>
              </div>
            </div>

            <div className="grid gap-3 sm:grid-cols-3">
              {workspaceMetrics.map((metric) => (
                <div key={metric.label} className="rounded-3xl border border-separator bg-surface/70 p-5 backdrop-blur">
                  <p className={`text-3xl font-black tracking-tight ${metric.className}`}>{metric.value}</p>
                  <p className="mt-2 text-sm font-medium text-muted">{metric.label}</p>
                </div>
              ))}
            </div>
          </div>
        </div>

        <Widget className="relative overflow-hidden border border-separator bg-foreground text-background shadow-2xl shadow-foreground/10 dark:bg-surface dark:text-foreground">
          <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_70%_0%,rgba(45,212,191,0.24),transparent_34%),radial-gradient(circle_at_0%_100%,rgba(124,58,237,0.22),transparent_34%)]" />
          <Widget.Header className="relative flex flex-col items-start gap-3">
            <div className="inline-flex rounded-full bg-background/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.22em] text-background/70 dark:bg-foreground/10 dark:text-muted">
              {t("home.nextMove")}
            </div>
            <Widget.Title className="text-3xl font-black leading-tight text-background dark:text-foreground">
              {t("home.exploreModuleDemo")}
            </Widget.Title>
          </Widget.Header>
          <Widget.Content className="relative flex flex-col gap-6 text-sm leading-7 text-background/70 dark:text-muted">
            <p>{t("home.firstDemoDescription")}</p>
            <div className="grid gap-3">
              {demoModules.slice(0, 2).map((module) => (
                <Link key={module.href} href={module.href} className="group rounded-2xl border border-background/10 bg-background/10 p-4 transition hover:bg-background/15 dark:border-separator dark:bg-surface-secondary/70 dark:hover:bg-surface-secondary">
                  <span className="flex items-center justify-between gap-3">
                    <span className="font-semibold text-background dark:text-foreground">{module.title}</span>
                    <span className={isRtl ? "transition group-hover:-translate-x-1" : "transition group-hover:translate-x-1"} aria-hidden="true">
                      {isRtl ? "←" : "→"}
                    </span>
                  </span>
                  <span className="mt-1 block text-xs uppercase tracking-[0.18em] text-background/50 dark:text-muted">{module.metric}</span>
                </Link>
              ))}
            </div>
          </Widget.Content>
        </Widget>
      </section>

      <section aria-labelledby="module-demos" className="flex flex-col gap-5 rounded-[2rem] border border-separator bg-surface/75 p-5 shadow-xl shadow-foreground/5 backdrop-blur md:p-8">
        <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
          <div className="flex flex-col gap-2">
            <p className="text-sm font-semibold uppercase tracking-[0.3em] text-muted">{t("home.availableModules")}</p>
            <h2 id="module-demos" className="text-3xl font-black tracking-tight text-foreground md:text-4xl">
              {t("home.chooseNext")}
            </h2>
          </div>
          <p className="max-w-xl text-sm leading-6 text-muted">
            {t("home.cardsSourceNote")}
          </p>
        </div>

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {demoModules.map((module) => (
            <Link key={module.href} href={module.href} className="group block h-full">
              <ItemCard className="relative h-full overflow-hidden border-separator bg-surface transition duration-300 hover:-translate-y-1 hover:bg-surface-secondary hover:shadow-2xl hover:shadow-foreground/10" variant="outline">
                <div className={`pointer-events-none absolute inset-x-0 top-0 h-32 bg-gradient-to-br ${module.accentClassName}`} />
                <ItemCard.Content className="relative gap-5">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex h-12 w-12 items-center justify-center rounded-2xl border border-separator bg-surface/85 text-lg font-black shadow-sm backdrop-blur">
                      {module.icon}
                    </div>
                    <ItemCard.Description className="rounded-full bg-surface-secondary px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em]">
                      {module.metric}
                    </ItemCard.Description>
                  </div>
                  <div className="flex flex-col gap-3">
                    <ItemCard.Title className="text-2xl font-black tracking-tight">{module.title}</ItemCard.Title>
                    <p className="text-sm leading-6 text-muted">{module.description}</p>
                  </div>
                </ItemCard.Content>
                <ItemCard.Action className="relative">
                  <span className="inline-flex items-center gap-2 font-semibold text-foreground">
                    {module.callToAction}
                    <span className={isRtl ? "transition group-hover:-translate-x-1" : "transition group-hover:translate-x-1"} aria-hidden="true">
                      {isRtl ? "←" : "→"}
                    </span>
                  </span>
                </ItemCard.Action>
              </ItemCard>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}
