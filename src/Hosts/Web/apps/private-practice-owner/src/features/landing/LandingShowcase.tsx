"use client";

import { useEffect, useRef, useState } from "react";
import { useInView, useReducedMotion } from "motion/react";
import { Surface, Tabs } from "@heroui/react";
import { FaIcon, PageContainer } from "@vitarish/ui";
import { useLocalizedText } from "@/localization";
import { LandingFeatureMock, landingFeatureIds, type LandingFeatureId } from "./LandingFeatureMocks";
import { particleAnchor, particleSlot } from "./particleAnchors";
import styles from "./landing-showcase.module.css";

const [assistant, reports, bookings] = landingFeatureIds;
const chapters = [
  { id: assistant, formation: "sparkle", icon: "message-bot", tab: "practiceLanding.tabAssistant", title: "practiceLanding.featureAssistant", description: "practiceLanding.featureAssistantDescription" },
  { id: bookings, formation: "calendar", icon: "calendar-check", tab: "practiceLanding.tabBookings", title: "practiceLanding.featureBookings", description: "practiceLanding.featureBookingsDescription" },
  { id: reports, formation: "bars", icon: "chart-simple", tab: "practiceLanding.tabReports", title: "practiceLanding.featureReports", description: "practiceLanding.featureReportsDescription" },
] as const;

/** How long each example is shown while the visitor is only watching. */
const watchingMilliseconds = 7000;

/**
 * The whole product in one screen: three tabs, each with a working example. Choosing a tab also
 * changes the particle field's formation. Until the visitor touches the showcase it moves on by
 * itself, so someone who only watches still sees all three.
 */
export function LandingShowcase({ id }: { id: string }) {
  const { t } = useLocalizedText();
  const ref = useRef<HTMLElement>(null);
  const [selected, setSelected] = useState<LandingFeatureId>(chapters[0].id);
  const [watching, setWatching] = useState(true);
  const inView = useInView(ref, { amount: 0.4 });
  const reducedMotion = useReducedMotion();
  const advancing = watching && inView && !reducedMotion;
  const chapter = chapters.find((candidate) => candidate.id === selected) ?? chapters[0];
  const stopWatching = () => setWatching(false);

  useEffect(() => {
    if (!advancing) return;
    const timer = window.setInterval(() => {
      setSelected((current) => chapters[(chapters.findIndex((candidate) => candidate.id === current) + 1) % chapters.length].id);
    }, watchingMilliseconds);
    return () => window.clearInterval(timer);
  }, [advancing]);

  return (
    <section
      {...particleAnchor(chapter.formation)}
      ref={ref}
      id={id}
      aria-labelledby={`${id}-title`}
      className={styles.showcase}
      onPointerDownCapture={stopWatching}
      onKeyDownCapture={stopWatching}
    >
      <PageContainer width="content" className={styles.layout}>
        <div className={styles.story}>
          <div {...particleSlot} aria-hidden="true" className={styles.emblem} />
          <div key={chapter.id} className={styles.copy}>
            <h2 className={`${styles.title} font-tone-confident`} id={`${id}-title`}>{t(chapter.title)}</h2>
            <p className={styles.description}>{t(chapter.description)}</p>
          </div>
        </div>
        <Tabs
          className={styles.examples}
          selectedKey={selected}
          onSelectionChange={(key) => setSelected(chapters.find((candidate) => candidate.id === key)?.id ?? selected)}
        >
          <Tabs.ListContainer>
            <Tabs.List aria-label={t("practiceLanding.explore")}>
              {chapters.map((candidate) => (
                <Tabs.Tab key={candidate.id} id={candidate.id} className={styles.tab}>
                  <FaIcon aria-hidden="true" icon={candidate.icon} />
                  {t(candidate.tab)}
                  <Tabs.Indicator />
                  {advancing && candidate.id === selected && <span aria-hidden="true" className={styles.countdown} style={{ animationDuration: `${watchingMilliseconds}ms` }} />}
                </Tabs.Tab>
              ))}
            </Tabs.List>
          </Tabs.ListContainer>
          {chapters.map((candidate) => (
            <Tabs.Panel key={candidate.id} id={candidate.id} className={styles.panel}>
              <Surface className={styles.example} variant="secondary">
                <LandingFeatureMock feature={candidate.id} />
              </Surface>
            </Tabs.Panel>
          ))}
        </Tabs>
      </PageContainer>
    </section>
  );
}
