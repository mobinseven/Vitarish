"use client";

import { ViewportReveal } from "@vitarish/ui";

export function ViewportRevealTestClient() {
  return (
    <div
      data-testid="viewport-reveal-test-page"
      className="min-h-[220vh] bg-[#f5f5f7] px-6 py-12"
    >
      <div
        aria-hidden="true"
        className="mx-auto h-[105vh] max-w-5xl border-b border-black/10"
      />
      <ViewportReveal
        as="section"
        id="reveal"
        data-testid="viewport-reveal-target"
        aria-hidden="true"
        className="mx-auto mt-12 flex min-h-[420px] max-w-4xl items-center justify-center rounded-[2rem] bg-white p-10 sm:p-16"
      >
        <div className="relative aspect-[4/3] w-full max-w-lg rounded-[1.75rem] border border-black/15 bg-[#18191b] p-2 shadow-[0_24px_60px_-20px_rgba(0,0,0,0.3)]">
          <div className="h-full overflow-hidden rounded-[1.3rem] bg-[radial-gradient(ellipse_at_25%_25%,#dbeafe_0%,#a5b4fc_30%,#6366f1_55%,#312e81_85%)]">
            <div className="mx-auto mt-[12%] h-[65%] w-[72%] rounded-xl border border-white/50 bg-white/80 p-5 shadow-lg">
              <div className="mb-5 h-2 w-1/3 rounded-full bg-indigo-950/30" />
              <div className="grid h-3/5 grid-cols-3 gap-3">
                <div className="rounded-lg bg-indigo-200/80" />
                <div className="col-span-2 rounded-lg bg-white/90" />
              </div>
            </div>
          </div>
        </div>
      </ViewportReveal>
    </div>
  );
}
