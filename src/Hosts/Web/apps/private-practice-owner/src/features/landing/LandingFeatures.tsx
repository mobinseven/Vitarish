"use client";

import { useEffect, useRef, useState } from "react";
import { ViewportReveal } from "@vitarish/ui";
import { useLocalizedText } from "@/localization";
import { LandingFeatureMock, landingFeatureIds, type LandingFeatureId } from "./LandingFeatureMocks";
import styles from "./owner-landing.module.css";

const capitalize = (value: string) => value.charAt(0).toUpperCase() + value.slice(1);

// A section is "in view" while it crosses the middle band of the screen.
const toneBandMargin = "-45% 0px -45% 0px";

/** One section per capability: a short title, then a working sample of it. The ground takes each section's tone as it scrolls by. */
export function LandingFeatures() {
  const { t } = useLocalizedText();
  const [tone, setTone] = useState<LandingFeatureId>(landingFeatureIds[0]);
  const sectionRefs = useRef(new Map<LandingFeatureId, HTMLElement>());

  useEffect(() => {
    if (typeof IntersectionObserver === "undefined") return;
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;
          const id = landingFeatureIds.find((candidate) => sectionRefs.current.get(candidate) === entry.target);
          if (id) setTone(id);
        }
      },
      { rootMargin: toneBandMargin, threshold: 0 },
    );
    sectionRefs.current.forEach((element) => observer.observe(element));
    return () => observer.disconnect();
  }, []);

  return (
    <div className={styles.tones} data-tone={tone}>
      {landingFeatureIds.map((id) => {
        const key = capitalize(id);
        return (
          <section
            key={id}
            ref={(element) => {
              if (element) sectionRefs.current.set(id, element);
              else sectionRefs.current.delete(id);
            }}
            aria-labelledby={`landing-feature-${id}`}
            className={styles.feature}
          >
            <ViewportReveal className={styles.featureText}>
              <h2 className={styles.display} id={`landing-feature-${id}`}>{t(`practiceLanding.feature${key}`)}</h2>
              <p className={styles.featureLine}>{t(`practiceLanding.feature${key}Description`)}</p>
            </ViewportReveal>
            <ViewportReveal className={styles.featurePanel} delayMs={120}>
              <LandingFeatureMock feature={id} />
            </ViewportReveal>
          </section>
        );
      })}
    </div>
  );
}
