"use client";

import { Link } from "@heroui/react";
import {
  ActionGroup,
  AppShell,
  HeroPanel,
  SectionHeader,
  StatusBadge,
  SurfaceCard,
} from "@/design-system";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";

interface DemoModuleCard {
  title: string;
  description: string;
  href: string;
  metric: string;
  callToAction: string;
}

const demoModules: DemoModuleCard[] = [
  {
    title: "Knowledge",
    description:
      "Ask grounded questions across available corpora and continue exploring the sources behind every answer.",
    href: Routes.Knowledge.Index,
    metric: "Q&A demo",
    callToAction: "Open Knowledge",
  },
];

export function HomeDashboard() {
  const { user, isLoading } = useAuthSession();
  const userName = resolveUserName(
    user?.displayName,
    user?.email,
    user?.phoneNumber,
  );

  return (
    <AppShell>
      <HeroPanel>
        <div className="grid gap-8 lg:grid-cols-[1.2fr_0.8fr] lg:items-end">
          <div className="flex flex-col gap-6">
            <StatusBadge tone="success">Your workspace is ready</StatusBadge>

            <div className="flex flex-col gap-4">
              <p className="text-sm font-semibold uppercase tracking-[0.35em] text-muted">
                Vitarish Home
              </p>
              <h1 className="max-w-4xl text-4xl font-black tracking-tight text-foreground md:text-6xl">
                {isLoading ? "Welcome back" : `Welcome back, ${userName}`}
              </h1>
              <p className="max-w-2xl text-lg leading-8 text-muted md:text-xl">
                Pick a module demo below to keep moving. Each card opens a
                focused experience so you can explore what is available now
                without leaving your workflow.
              </p>
            </div>

            <ActionGroup>
              <Link
                href={Routes.Knowledge.Index}
                className="inline-flex min-h-12 items-center justify-center rounded-full bg-[var(--accent)] px-6 font-semibold text-[var(--accent-foreground)] shadow-lg transition hover:brightness-95"
              >
                Start with Knowledge
              </Link>
              <Link
                href={Routes.Account.Index}
                className="inline-flex min-h-12 items-center justify-center rounded-full border border-[var(--border)] bg-[var(--surface-glass)] px-6 font-semibold text-[var(--accent)] shadow-sm transition hover:bg-[var(--surface)]"
              >
                Review account
              </Link>
            </ActionGroup>
          </div>

          <SurfaceCard tone="elevated">
            <SurfaceCard.Header className="flex flex-col items-start gap-2">
              <p className="text-sm font-semibold uppercase tracking-[0.25em] text-muted">
                Next move
              </p>
              <h2 className="text-2xl font-bold">Explore a module demo</h2>
            </SurfaceCard.Header>
            <SurfaceCard.Content className="text-sm leading-6 text-muted">
              Knowledge is the first available demo module. More cards can be
              added here when additional module demo pages are introduced.
            </SurfaceCard.Content>
          </SurfaceCard>
        </div>
      </HeroPanel>

      <section aria-labelledby="module-demos" className="flex flex-col gap-4">
        <SectionHeader
          id="module-demos"
          eyebrow="Available modules"
          title="Choose where to go next"
          description="Cards are intentionally sourced from the React route definitions so navigation stays aligned with available pages."
        />

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {demoModules.map((module) => (
            <Link
              key={module.href}
              href={module.href}
              className="group block rounded-[1.5rem] focus:outline-none"
            >
              <SurfaceCard
                tone="interactive"
                className="h-full overflow-hidden"
              >
                <div className="h-28 bg-[linear-gradient(135deg,var(--accent-soft),var(--success-soft),color-mix(in_oklch,var(--warning)_14%,transparent))]" />
                <SurfaceCard.Header className="flex flex-col items-start gap-3">
                  <span className="rounded-full bg-[var(--accent-soft)] px-3 py-1 text-xs font-semibold uppercase tracking-[0.2em] text-[var(--accent)]">
                    {module.metric}
                  </span>
                  <h3 className="text-2xl font-bold text-foreground">
                    {module.title}
                  </h3>
                </SurfaceCard.Header>
                <SurfaceCard.Content className="flex flex-1 flex-col justify-between gap-6 text-sm leading-6 text-muted">
                  <p>{module.description}</p>
                  <span className="inline-flex items-center gap-2 font-semibold text-[var(--accent)] transition group-hover:gap-3">
                    {module.callToAction}
                    <span aria-hidden="true">→</span>
                  </span>
                </SurfaceCard.Content>
              </SurfaceCard>
            </Link>
          ))}
        </div>
      </section>
    </AppShell>
  );
}

function resolveUserName(
  displayName?: string | null,
  email?: string | null,
  phoneNumber?: string | null,
): string {
  const normalizedDisplayName = displayName?.trim();
  if (normalizedDisplayName) {
    return normalizedDisplayName;
  }

  const emailName = email?.split("@")?.[0]?.trim();
  if (emailName) {
    return emailName;
  }

  const normalizedPhoneNumber = phoneNumber?.trim();
  if (normalizedPhoneNumber) {
    return normalizedPhoneNumber;
  }

  return "friend";
}
