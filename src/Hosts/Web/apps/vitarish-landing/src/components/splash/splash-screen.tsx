"use client";

import { useEffect, useState } from "react";
import { useLocalizedText } from "@vitarish/localization";
import { LoadingSpinner } from "@vitarish/ui";
import styles from "./splash-screen.module.css";

// Long enough for the wordmark to be seen even when the page is already cached.
const splashMinimumMilliseconds = 1600;
const brandWordmark = "VITARISH";
/** Lets the layout hide the splash for visitors without scripts, who could never dismiss it. */
export const splashAttribute = "data-splash";

/** The first thing the page paints: covers it until the document has loaded, then fades away. */
export function SplashScreen() {
  const { t } = useLocalizedText();
  const [isAnimated, setIsAnimated] = useState(false);
  const [isFading, setIsFading] = useState(false);
  const [isGone, setIsGone] = useState(false);

  useEffect(() => {
    setIsAnimated(true);
    const startedAt = performance.now();
    let timer: number | undefined;
    const finish = () => {
      const remaining = Math.max(0, splashMinimumMilliseconds - (performance.now() - startedAt));
      timer = window.setTimeout(() => setIsFading(true), remaining);
    };
    if (document.readyState === "complete") finish();
    else window.addEventListener("load", finish, { once: true });
    return () => {
      window.removeEventListener("load", finish);
      window.clearTimeout(timer);
    };
  }, []);

  if (isGone) return null;
  return (
    <div
      aria-label={t("landing.loading.label")}
      className={styles.splash}
      {...{ [splashAttribute]: "" }}
      data-fading={isFading}
      onTransitionEnd={() => setIsGone(true)}
      role="status"
    >
      {isAnimated ? <LoadingSpinner /> : <span className={styles.wordmark}>{brandWordmark}</span>}
    </div>
  );
}
