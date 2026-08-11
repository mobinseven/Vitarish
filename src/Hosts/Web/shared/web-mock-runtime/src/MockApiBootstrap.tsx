"use client";

import { type RequestHandler } from "msw";
import { type ReactNode, useEffect, useState } from "react";
import { createDefaultMockHandlers, resolveMockScenario } from "./mockHandlers";

const mockApiEnabled = process.env.NEXT_PUBLIC_VITARISH_MOCK_API === "1";
const requestedScenario = process.env.NEXT_PUBLIC_VITARISH_MOCK_SCENARIO;
const emptyHandlers: readonly RequestHandler[] = [];

export function MockApiBootstrap({
  children,
  handlers = emptyHandlers,
}: {
  children: ReactNode;
  handlers?: readonly RequestHandler[];
}) {
  const [status, setStatus] = useState<"starting" | "ready" | "failed">(
    mockApiEnabled ? "starting" : "ready",
  );

  useEffect(() => {
    if (!mockApiEnabled) return;
    let active = true;
    let workerInstance: { stop: () => void } | null = null;

    void import("msw/browser")
      .then(async ({ setupWorker }) => {
        const worker = setupWorker(
          ...handlers,
          ...createDefaultMockHandlers(resolveMockScenario(requestedScenario)),
        );
        workerInstance = worker;
        await worker.start({
          onUnhandledRequest: (request, print) => {
            // Next.js App Router emits same-origin RSC/navigation requests that
            // must reach the dev server. Keep the strict failure boundary for
            // application API calls so a mock cannot silently use a live API.
            const pathname = new URL(request.url).pathname;
            if (pathname.startsWith("/api/")) {
              print.error();
            }
          },
          serviceWorker: { url: "/mockServiceWorker.js" },
        });
        if (active) setStatus("ready");
        else worker.stop();
      })
      .catch((error: unknown) => {
        console.error("Unable to start Vitarish Web mock API.", error);
        if (active) setStatus("failed");
      });

    return () => {
      active = false;
      workerInstance?.stop();
    };
  }, [handlers]);

  if (status === "starting") {
    return <div role="status">{"Starting deterministic mock API…"}</div>;
  }

  if (status === "failed") {
    return <div role="alert">{"Unable to start the deterministic mock API. Check the browser console."}</div>;
  }

  return <>{children}</>;
}
