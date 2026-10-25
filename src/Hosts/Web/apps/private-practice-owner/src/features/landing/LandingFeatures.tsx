"use client";

import { Surface } from "@heroui/react";
import { ViewportReveal } from "@vitarish/ui";
import { useLocalizedText } from "@/localization";
import { LandingFeatureMock, landingFeatureIds } from "./LandingFeatureMocks";
import styles from "./owner-landing.module.css";

const capitalize = (value: string) => value.charAt(0).toUpperCase() + value.slice(1);

/** One section per capability on the plain ground: a short title beside a working sample of it, sides alternating. */
export function LandingFeatures() {
  const { t } = useLocalizedText();

  return (
    <div className={styles.features}>
      {landingFeatureIds.map((id) => {
        const key = capitalize(id);
        return (
          <section key={id} aria-labelledby={`landing-feature-${id}`} className={styles.feature}>
            <ViewportReveal className={styles.featureText}>
              <h2 className={`${styles.display} font-tone-confident`} id={`landing-feature-${id}`}>
                {t(`practiceLanding.feature${key}`)}
              </h2>
              <p className={styles.featureLine}>{t(`practiceLanding.feature${key}Description`)}</p>
            </ViewportReveal>
            <ViewportReveal className={styles.featurePanel} delayMs={120}>
              {/* The chat sits on the secondary surface so its white message bubbles stand out. */}
              <Surface className="rounded-3xl p-5 sm:p-6" variant={id === "assistant" ? "secondary" : "default"}>
                <LandingFeatureMock feature={id} />
              </Surface>
            </ViewportReveal>
          </section>
        );
      })}
    </div>
  );
}
