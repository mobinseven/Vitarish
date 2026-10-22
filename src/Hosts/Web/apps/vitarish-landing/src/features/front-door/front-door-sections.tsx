"use client";

import { Alert, Button, Chip, Skeleton, Tooltip } from "@heroui/react";
import { Timeline } from "@heroui-pro/react";
import { useLocalizedText } from "@vitarish/localization";
import {
  FaIcon,
  FeatureItemCard,
  FeatureItemGroup,
  MarketingSection,
  MarketingSectionHeading,
} from "@vitarish/ui";
import { availabilityTone, useLandingCatalog } from "./use-landing-catalog";
import styles from "./front-door.module.css";

/** Platform-level spotlight: the live clinic app first, then what is planned. */
export function Spotlight() {
  const { t } = useLocalizedText();
  const planned = [
    { key: "salons", icon: "spa" },
    { key: "dictionary", icon: "book" },
  ] as const;

  return (
    <section aria-label={t("landing.spotlight.label")} className={styles.spotlight}>
      <div className={styles.spotlightLead}>
        <Chip color="success" size="sm" variant="soft">{t("landing.availability.Live")}</Chip>
        <h2 className={styles.spotlightTitle}>
          <FaIcon aria-hidden="true" icon="hospital" />
          {t("landing.spotlight.clinic.title")}
        </h2>
        <p className={styles.readable}>{t("landing.spotlight.clinic.body")}</p>
      </div>
      {planned.map((item) => (
        <div key={item.key} className={styles.spotlightItem}>
          <Chip size="sm" variant="soft">{t("landing.availability.Planned")}</Chip>
          <h3 className={styles.spotlightItemTitle}>
            <FaIcon aria-hidden="true" icon={item.icon} />
            {t(`landing.spotlight.${item.key}.title`)}
          </h3>
          <p className={styles.readable}>{t(`landing.spotlight.${item.key}.body`)}</p>
        </div>
      ))}
    </section>
  );
}

const dayItems = [
  { key: "shifts", icon: "calendar-week" },
  { key: "booking", icon: "calendar-check" },
  { key: "checkIn", icon: "message-sms" },
  { key: "note", icon: "microphone" },
  { key: "prescription", icon: "file-prescription" },
  { key: "reminders", icon: "bell" },
] as const;

/** A day at a clinic; each step can start the conversation about itself. */
export function DayAtClinic({ onAsk }: { onAsk: (question: string) => void }) {
  const { t } = useLocalizedText();

  return (
    <MarketingSection id="day">
      <MarketingSectionHeading
        description={t("landing.day.description")}
        eyebrow={t("landing.day.eyebrow")}
        title={t("landing.day.title")}
      />
      <Timeline className={styles.dayTimeline} density="comfortable" size="md">
        {dayItems.map((item) => (
          <Timeline.Item key={item.key}>
            <Timeline.Marker aria-hidden="true">
              <FaIcon icon={item.icon} />
            </Timeline.Marker>
            <Timeline.Content>
              <div className={styles.dayItem}>
                <div>
                  <h3 className={styles.dayItemTitle}>{t(`landing.day.items.${item.key}.title`)}</h3>
                  <p className={styles.readable}>{t(`landing.day.items.${item.key}.body`)}</p>
                </div>
                <Tooltip>
                  <Button size="sm" variant="ghost" onPress={() => onAsk(t(`landing.day.items.${item.key}.question`))}>
                    <FaIcon aria-hidden="true" icon="comment-dots" />
                    {t("landing.day.ask")}
                  </Button>
                  <Tooltip.Content>{t("landing.day.askTooltip")}</Tooltip.Content>
                </Tooltip>
              </div>
            </Timeline.Content>
          </Timeline.Item>
        ))}
      </Timeline>
    </MarketingSection>
  );
}

const trustItems = [
  { key: "roles", icon: "user-shield" },
  { key: "audit", icon: "clock-rotate-left" },
  { key: "audio", icon: "microphone-slash" },
  { key: "address", icon: "globe" },
] as const;

export function TrustSection() {
  const { t } = useLocalizedText();

  return (
    <MarketingSection id="trust">
      <MarketingSectionHeading
        description={t("landing.trust.description")}
        eyebrow={t("landing.trust.eyebrow")}
        title={t("landing.trust.title")}
      />
      <FeatureItemGroup className={styles.cardGrid}>
        {trustItems.map((item) => (
          <FeatureItemCard key={item.key}>
            <FeatureItemCard.Icon>
              <FaIcon aria-hidden="true" icon={item.icon} />
            </FeatureItemCard.Icon>
            <FeatureItemCard.Content className={styles.wrapText}>
              <FeatureItemCard.Title>{t(`landing.trust.items.${item.key}.title`)}</FeatureItemCard.Title>
              <FeatureItemCard.Description className={styles.wrapText}>{t(`landing.trust.items.${item.key}.body`)}</FeatureItemCard.Description>
            </FeatureItemCard.Content>
          </FeatureItemCard>
        ))}
      </FeatureItemGroup>
    </MarketingSection>
  );
}

/** The honest roadmap, read from the same catalog as the assistant. */
export function Roadmap() {
  const { t } = useLocalizedText();
  const catalog = useLandingCatalog();

  return (
    <MarketingSection id="roadmap">
      <MarketingSectionHeading
        description={t("landing.roadmap.description")}
        eyebrow={t("landing.roadmap.eyebrow")}
        title={t("landing.roadmap.title")}
      />
      {catalog.isError ? (
        <Alert className={styles.sectionBody} status="danger">
          <Alert.Content>
            <Alert.Description>{t("landing.roadmap.error")}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : (
        <ul aria-busy={catalog.isPending} aria-label={t("landing.roadmap.title")} className={styles.featureList}>
          {catalog.data
            ? catalog.data.features.map((feature) => (
                <li key={feature.key} className={styles.featureRow}>
                  <div>
                    <h3 className={styles.featureTitle}>{t(`landing.features.${feature.key}.title`)}</h3>
                    <p className={styles.readable}>{t(`landing.features.${feature.key}.body`)}</p>
                  </div>
                  <Chip color={availabilityTone(feature.availability)} size="sm" variant="soft">
                    {t(`landing.availability.${feature.availability}`)}
                  </Chip>
                </li>
              ))
            : Array.from({ length: 4 }, (_, index) => (
                <li key={index} className={styles.featureRow}>
                  <Skeleton aria-label={t("landing.roadmap.loading")} className={styles.skeletonRow} />
                </li>
              ))}
        </ul>
      )}
    </MarketingSection>
  );
}
