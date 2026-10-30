"use client";

import Image from "next/image";
import { Surface } from "@heroui/react";
import { FaIcon, PageContainer, ViewportReveal } from "@vitarish/ui";
import { formatNumber, useLocalizedText } from "@/localization";
import { LandingFeatureMock, landingFeatureIds } from "./LandingFeatureMocks";
import styles from "./owner-landing.module.css";

const [assistant, reports, bookings] = landingFeatureIds;
const chapters = [
  { id: bookings, title: "practiceLanding.featureBookings", description: "practiceLanding.featureBookingsDescription", moment: "practiceLanding.momentBookings", hint: "practiceLanding.tryBookings", icon: "calendar-check" },
  { id: assistant, title: "practiceLanding.featureAssistant", description: "practiceLanding.featureAssistantDescription", moment: "practiceLanding.momentAssistant", hint: "practiceLanding.tryAssistant", icon: "message-bot" },
  { id: reports, title: "practiceLanding.featureReports", description: "practiceLanding.featureReportsDescription", moment: "practiceLanding.momentReports", hint: "practiceLanding.tryReports", icon: "chart-simple" },
] as const;

/** Full-width scenes keep the existing examples interactive and in normal reading order. */
export function LandingFeatures({ interiorArtwork }: { interiorArtwork: string }) {
  const { t, locale } = useLocalizedText();

  return (
    <div className={styles.features}>
      {chapters.map(({ id, title, description, moment, hint, icon }, index) => (
        <Surface render={(props) => <section {...props} />} key={id} aria-labelledby={`landing-feature-${id}`} className={styles.feature}>
          <PageContainer width="content" className={styles.featureContent}>
            <ViewportReveal className={styles.featureText}>
              <p className={styles.chapterLabel}>
                <span className={styles.chapterNumber}>{formatNumber(index + 1, locale, { minimumIntegerDigits: 2 })}</span>
                <span>{t(moment)}</span>
              </p>
              <h2 className={`${styles.display} font-tone-confident`} id={`landing-feature-${id}`}>{t(title)}</h2>
              <p className={styles.featureLine}>{t(description)}</p>
              <p className={styles.tryHint}><FaIcon aria-hidden="true" icon={icon} />{t(hint)}</p>
            </ViewportReveal>
            <ViewportReveal className={styles.featurePanel} delayMs={100}>
              {id === assistant && (
                <div aria-hidden="true" className={styles.sampleBackdrop}>
                  <Image alt="" className={styles.artImage} src={interiorArtwork} fill sizes="(min-width: 1024px) 50vw, 100vw" />
                </div>
              )}
              <div aria-hidden="true" className={styles.sampleArch} />
              <Surface className={styles.sample} variant="secondary">
                <LandingFeatureMock feature={id} />
              </Surface>
            </ViewportReveal>
          </PageContainer>
        </Surface>
      ))}
    </div>
  );
}
