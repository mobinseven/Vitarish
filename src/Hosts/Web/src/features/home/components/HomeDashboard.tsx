"use client";

import { Card, Link } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";

interface DemoModuleCard {
  title: string;
  description: string;
  href: string;
  accent: string;
  metric: string;
  callToAction: string;
}

const demoModules: DemoModuleCard[] = [
  {
    title: "Knowledge",
    description: "Ask grounded questions across available corpora and continue exploring the sources behind every answer.",
    href: Routes.Knowledge.Index,
    accent: "from-violet-500/20 via-fuchsia-500/10 to-cyan-500/20",
    metric: "Q&A demo",
    callToAction: "Open Knowledge",
  },
];

export function HomeDashboard() {
  const { displayProfile, isLoading } = useAuthSession();

  return (
    <main className="auth-bg min-h-screen overflow-hidden px-4 py-8 md:px-8 lg:px-12">
      <div className="mx-auto flex max-w-7xl flex-col gap-8">
        <section className="relative overflow-hidden rounded-[2rem] border border-white/20 bg-gradient-to-br from-white/80 via-white/60 to-white/30 p-6 shadow-2xl shadow-violet-950/10 backdrop-blur-xl dark:border-white/10 dark:from-zinc-950/80 dark:via-zinc-900/60 dark:to-violet-950/20 md:p-10">
          <div className="absolute -right-20 -top-24 h-72 w-72 rounded-full bg-violet-500/20 blur-3xl" />
          <div className="absolute -bottom-28 left-1/3 h-72 w-72 rounded-full bg-cyan-400/20 blur-3xl" />

          <div className="relative grid gap-8 lg:grid-cols-[1.2fr_0.8fr] lg:items-end">
            <div className="flex flex-col gap-6">
              <div className="inline-flex w-fit items-center gap-2 rounded-full border border-violet-300/50 bg-white/60 px-4 py-2 text-sm font-medium text-violet-700 shadow-sm backdrop-blur dark:border-violet-300/20 dark:bg-white/10 dark:text-violet-200">
                <span className="h-2 w-2 rounded-full bg-emerald-400 shadow-[0_0_18px] shadow-emerald-400" />
                Your workspace is ready
              </div>

              <div className="flex flex-col gap-4">
                <p className="text-sm font-semibold uppercase tracking-[0.35em] text-muted">
                  Vitarish Home
                </p>
                <h1 className="max-w-4xl text-4xl font-black tracking-tight text-foreground md:text-6xl">
                  {isLoading ? "Welcome back" : `Welcome back, ${displayProfile.displayLabel}`}
                </h1>
                <p className="max-w-2xl text-lg leading-8 text-muted md:text-xl">
                  Pick a module demo below to keep moving. Each card opens a focused experience so you can explore what is available now without leaving your workflow.
                </p>
              </div>

              <div className="flex flex-col gap-3 sm:flex-row">
                <Link
                  href={Routes.Knowledge.Index}
                  className="inline-flex min-h-12 items-center justify-center rounded-full bg-violet-600 px-6 font-semibold text-white shadow-lg shadow-violet-600/25 transition hover:bg-violet-700"
                >
                  Start with Knowledge
                </Link>
                <Link
                  href={Routes.Account.Index}
                  className="inline-flex min-h-12 items-center justify-center rounded-full border border-violet-300/50 bg-white/70 px-6 font-semibold text-violet-700 shadow-sm transition hover:bg-white dark:border-white/10 dark:bg-white/10 dark:text-violet-100 dark:hover:bg-white/15"
                >
                  Review account
                </Link>
              </div>
            </div>

            <Card className="glass-card relative border-white/30 bg-white/70 dark:border-white/10 dark:bg-zinc-950/50">
              <Card.Header className="flex flex-col items-start gap-2">
                <p className="text-sm font-semibold uppercase tracking-[0.25em] text-muted">Next move</p>
                <h2 className="text-2xl font-bold">Explore a module demo</h2>
              </Card.Header>
              <Card.Content className="text-sm leading-6 text-muted">
                Knowledge is the first available demo module. More cards can be added here when additional module demo pages are introduced.
              </Card.Content>
            </Card>
          </div>
        </section>

        <section aria-labelledby="module-demos" className="flex flex-col gap-4">
          <div className="flex flex-col justify-between gap-2 md:flex-row md:items-end">
            <div>
              <p className="text-sm font-semibold uppercase tracking-[0.3em] text-muted">Available modules</p>
              <h2 id="module-demos" className="text-3xl font-bold text-foreground">Choose where to go next</h2>
            </div>
            <p className="max-w-xl text-sm leading-6 text-muted">
              Cards are intentionally sourced from the React route definitions so navigation stays aligned with available pages.
            </p>
          </div>

          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
            {demoModules.map((module) => (
              <Link key={module.href} href={module.href} className="group block rounded-[1.5rem] focus:outline-none focus:ring-2 focus:ring-violet-500 focus:ring-offset-2 focus:ring-offset-background">
                <Card className="glass-card h-full overflow-hidden transition duration-300 group-hover:-translate-y-1 group-hover:shadow-2xl group-hover:shadow-violet-950/15">
                  <div className={`h-28 bg-gradient-to-br ${module.accent}`} />
                  <Card.Header className="flex flex-col items-start gap-3">
                    <span className="rounded-full bg-violet-500/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.2em] text-violet-700 dark:text-violet-200">
                      {module.metric}
                    </span>
                    <h3 className="text-2xl font-bold text-foreground">{module.title}</h3>
                  </Card.Header>
                  <Card.Content className="flex flex-1 flex-col justify-between gap-6 text-sm leading-6 text-muted">
                    <p>{module.description}</p>
                    <span className="inline-flex items-center gap-2 font-semibold text-violet-700 transition group-hover:gap-3 dark:text-violet-200">
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
