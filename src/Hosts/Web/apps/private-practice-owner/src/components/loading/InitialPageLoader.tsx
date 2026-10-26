"use client";

import { useEffect, useState } from "react";
import { useAuthSession } from "@vitarish/auth";
import { LoadingScreen } from "./LoadingScreen";

export function InitialPageLoader() {
  const { isLoading } = useAuthSession();
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    if (isLoading) return;

    let cancelled = false;
    let frame: number | undefined;
    const waitForFonts = () => {
      frame = window.requestAnimationFrame(() => {
        // Discover text and pseudo-element fonts in the newly rendered page
        // before reading ready; the loader itself uses neither Ahang nor icons.
        document.body.getBoundingClientRect();
        void document.fonts.ready.then(() => {
          // ready also settles when a font fails, allowing the CSS fallback.
          if (!cancelled) setIsReady(true);
        });
      });
    };

    if (document.readyState === "complete") waitForFonts();
    else window.addEventListener("load", waitForFonts, { once: true });

    return () => {
      cancelled = true;
      window.removeEventListener("load", waitForFonts);
      if (frame !== undefined) window.cancelAnimationFrame(frame);
    };
  }, [isLoading]);

  if (isReady) return null;

  return (
    <div
      id="initial-page-loader"
      className="fixed inset-0 z-[9999] bg-background"
    >
      <LoadingScreen />
    </div>
  );
}
