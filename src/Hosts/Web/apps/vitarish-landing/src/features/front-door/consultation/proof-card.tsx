"use client";

import { useState, type ReactNode } from "react";
import { Alert, Chip, Label, NumberField, Surface } from "@heroui/react";
import { Timeline } from "@heroui-pro/react";
import { formatDate, formatNumber, formatTimeOfDay, useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { LandingProofKind } from "@vitarish/platform";
import styles from "../front-door.module.css";

const weeksPerMonth = 52 / 12;
const sampleSlots = ["09:30", "11:00", "16:30"];
const sampleWeekdays = [1, 2, 3, 4, 5];

/** What empty slots cost, worked out only from the numbers the owner types; nothing is stored. */
function NoShowCost() {
  const { t, locale } = useLocalizedText();
  const [missed, setMissed] = useState(4);
  const [fee, setFee] = useState(0);
  const monthly = Math.round(missed * fee * weeksPerMonth);

  return (
    <div className={styles.proofBody}>
      <div className={styles.proofFields}>
        <NumberField minValue={0} value={missed} onChange={(value) => setMissed(Number.isFinite(value) ? value : 0)}>
          <Label>{t("landing.consultation.proofs.NoShowCost.missed")}</Label>
          <NumberField.Group>
            <NumberField.DecrementButton />
            <NumberField.Input />
            <NumberField.IncrementButton />
          </NumberField.Group>
        </NumberField>
        <NumberField minValue={0} value={fee} onChange={(value) => setFee(Number.isFinite(value) ? value : 0)}>
          <Label>{t("landing.consultation.proofs.NoShowCost.fee")}</Label>
          <NumberField.Group>
            <NumberField.Input />
          </NumberField.Group>
        </NumberField>
      </div>
      {fee > 0 ? (
        <p aria-live="polite" className={styles.proofResult}>
          {t("landing.consultation.proofs.NoShowCost.monthly", { amount: formatNumber(monthly, locale) })}
        </p>
      ) : null}
    </div>
  );
}

function BookingPreview({ kindLabel }: { kindLabel: string }) {
  const { t, locale } = useLocalizedText();

  return (
    <Surface className={styles.proofPreview} variant="secondary">
      <span className={styles.proofPreviewTitle}>{t("landing.consultation.proofs.BookingPreview.clinic", { kind: kindLabel })}</span>
      <span className={styles.panelLabel}>{t("landing.consultation.proofs.BookingPreview.slotsLabel")}</span>
      <div className={styles.slots}>
        {sampleSlots.map((slot) => (
          <span key={slot} className={styles.slot}>
            <FaIcon aria-hidden="true" icon="clock" />
            {formatTimeOfDay(slot, locale)}
          </span>
        ))}
      </div>
    </Surface>
  );
}

function PatientTimeline() {
  const { t } = useLocalizedText();
  const visits = ["first", "procedure", "followUp"] as const;

  return (
    <Timeline density="compact" size="sm">
      {visits.map((visit, index) => (
        <Timeline.Item key={visit} status={index === visits.length - 1 ? "current" : "default"}>
          <Timeline.Marker aria-hidden="true">
            <FaIcon icon="stethoscope" />
          </Timeline.Marker>
          <Timeline.Content>{t(`landing.consultation.proofs.PatientTimeline.visits.${visit}`)}</Timeline.Content>
        </Timeline.Item>
      ))}
    </Timeline>
  );
}

function Dictation({ voiceAction }: { voiceAction: ReactNode }) {
  const { t } = useLocalizedText();

  return (
    <div className={styles.proofBody}>
      <blockquote className={styles.dictation}>
        <FaIcon aria-hidden="true" icon="microphone" />
        <span>{t("landing.consultation.proofs.Dictation.sample")}</span>
      </blockquote>
      {voiceAction}
    </div>
  );
}

function ShiftWeek() {
  const { locale } = useLocalizedText();
  // A Monday-to-Friday week in a fixed month, named in the visitor's own calendar and language.
  const days = sampleWeekdays.map((offset) => formatDate(Date.UTC(2026, 0, 4 + offset), locale, { weekday: "short", timeZone: "UTC" }));

  return (
    <div className={styles.week}>
      {days.map((day) => (
        <span key={day} className={styles.weekDay}>
          <span>{day}</span>
          <span className={styles.weekShift}>
            {formatTimeOfDay("09:00", locale)}–{formatTimeOfDay("17:00", locale)}
          </span>
        </span>
      ))}
    </div>
  );
}

/** One example of a feature working, chosen from the pains the owner picked. */
export function ProofCard({ kind, kindLabel, voiceAction }: { kind: LandingProofKind; kindLabel: string; voiceAction: ReactNode }) {
  const { t } = useLocalizedText();
  if (kind === LandingProofKind.InProgress) {
    return (
      <Alert status="warning">
        <Alert.Indicator />
        <Alert.Content>
          <Alert.Title>{t("landing.consultation.proofs.InProgress.title")}</Alert.Title>
          <Alert.Description>{t("landing.consultation.proofs.InProgress.body")}</Alert.Description>
        </Alert.Content>
      </Alert>
    );
  }

  return (
    <Surface className={styles.proof} variant="secondary">
      <div className={styles.proofHeader}>
        <h3 className={styles.proofTitle}>{t(`landing.consultation.proofs.${kind}.title`)}</h3>
        <Chip size="sm" variant="soft">{t("landing.consultation.example")}</Chip>
      </div>
      <p className={styles.readable}>{t(`landing.consultation.proofs.${kind}.body`)}</p>
      {kind === LandingProofKind.NoShowCost ? <NoShowCost /> : null}
      {kind === LandingProofKind.BookingPreview ? <BookingPreview kindLabel={kindLabel} /> : null}
      {kind === LandingProofKind.PatientTimeline ? <PatientTimeline /> : null}
      {kind === LandingProofKind.Dictation ? <Dictation voiceAction={voiceAction} /> : null}
      {kind === LandingProofKind.ShiftWeek ? <ShiftWeek /> : null}
    </Surface>
  );
}
