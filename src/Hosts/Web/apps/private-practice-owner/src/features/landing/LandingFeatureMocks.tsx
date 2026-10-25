"use client";

import { useState, type ReactNode } from "react";
import { Button, Chip, Tooltip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { KPI } from "@heroui-pro/react/kpi";
import { KPIGroup } from "@heroui-pro/react/kpi-group";
import { ChatMessage, FaIcon } from "@vitarish/ui";
import { formatPercent, formatTimeOfDay, useLocalizedText, type MessageKey } from "@/localization";

/** The mocks run on fixed sample data and never call the API. */
export const landingFeatureIds = ["assistant", "reports", "bookings"] as const;
export type LandingFeatureId = (typeof landingFeatureIds)[number];

function MockAction({
  icon,
  label,
  tooltip,
  onPress,
  variant = "tertiary",
}: {
  icon: string;
  label: string;
  tooltip: string;
  onPress: () => void;
  variant?: "tertiary" | "outline";
}) {
  return (
    <Tooltip>
      <Button size="sm" variant={variant} onPress={onPress}>
        <FaIcon aria-hidden="true" icon={icon} />
        {label}
      </Button>
      <Tooltip.Content>{tooltip}</Tooltip.Content>
    </Tooltip>
  );
}

const chatSlots = [
  { id: "tuesday", time: "10:30", labelKey: "practiceLanding.mockChatSlotTuesday" },
  { id: "thursday", time: "16:00", labelKey: "practiceLanding.mockChatSlotThursday" },
] as const satisfies readonly { id: string; time: string; labelKey: MessageKey }[];

/** A patient asks for a visit; the clinic assistant offers openings and books the one picked. */
function AssistantMock() {
  const { t, locale } = useLocalizedText();
  const [pickedId, setPickedId] = useState<string | null>(null);
  const slotLabel = (slot: (typeof chatSlots)[number]) => t(slot.labelKey, { time: formatTimeOfDay(slot.time, locale) });
  const picked = chatSlots.find((slot) => slot.id === pickedId);
  const assistant = { avatarAlt: t("practiceLanding.mockAssistantName"), avatarFallback: t("practiceLanding.mockAssistantAvatar") };

  return (
    <div aria-live="polite" className="flex flex-col gap-5">
      <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
        <FaIcon aria-hidden="true" icon="message-bot" />
        {t("practiceLanding.mockAssistantName")}
        <span className="flex items-center gap-1.5 text-xs font-medium text-success">
          <span aria-hidden="true" className="size-2 rounded-full bg-success" />
          {t("practiceLanding.mockAssistantOnline")}
        </span>
      </div>
      <ChatMessage kind="question">{t("practiceLanding.mockChatQuestion")}</ChatMessage>
      <ChatMessage {...assistant} kind="answer">
        <div className="flex flex-col gap-3">
          <p className="text-sm text-foreground">{t("practiceLanding.mockChatOffer")}</p>
          {!picked && (
            <div className="flex flex-wrap gap-2">
              {chatSlots.map((slot) => (
                <MockAction
                  key={slot.id}
                  icon="calendar-check"
                  label={slotLabel(slot)}
                  tooltip={t("practiceLanding.mockChatSlotTooltip")}
                  variant="outline"
                  onPress={() => setPickedId(slot.id)}
                />
              ))}
            </div>
          )}
        </div>
      </ChatMessage>
      {picked && (
        <>
          <ChatMessage kind="question">{t("practiceLanding.mockChatPick", { slot: slotLabel(picked) })}</ChatMessage>
          <ChatMessage {...assistant} kind="notice" status={t("practiceLanding.mockChatBooked")} tone="success">
            <div className="flex flex-col items-start gap-3">
              <p className="flex items-start gap-2 text-sm text-foreground">
                <FaIcon aria-hidden="true" icon="circle-check" className="mt-0.5 shrink-0 text-success" />
                {t("practiceLanding.mockChatBookedBody", { slot: slotLabel(picked) })}
              </p>
              <MockAction
                icon="rotate-left"
                label={t("practiceLanding.mockChatRestart")}
                tooltip={t("practiceLanding.mockChatRestartTooltip")}
                onPress={() => setPickedId(null)}
              />
            </div>
          </ChatMessage>
        </>
      )}
    </div>
  );
}

// A rising week: each figure gains on last week, so every trend reads as success.
const reportFigures = [
  { id: "visits", icon: "stethoscope", value: 86, change: 0.12, labelKey: "practiceLanding.mockReportsVisits", trend: [52, 58, 55, 63, 68, 72, 86] },
  { id: "newPatients", icon: "user-plus", value: 14, change: 0.08, labelKey: "practiceLanding.mockReportsNewPatients", trend: [6, 8, 7, 9, 11, 12, 14] },
  { id: "returning", icon: "rotate", value: 0.68, change: 0.05, labelKey: "practiceLanding.mockReportsReturning", trend: [54, 57, 59, 58, 62, 65, 68], isRate: true },
] as const satisfies readonly {
  id: string;
  icon: string;
  value: number;
  change: number;
  labelKey: MessageKey;
  trend: readonly number[];
  isRate?: boolean;
}[];

/** This week's practice figures, each with its trend against last week. */
function ReportsMock() {
  const { t, locale } = useLocalizedText();
  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("practiceLanding.mockReportsTitle")}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        <KPIGroup className="grid gap-3 sm:grid-cols-3">
          {reportFigures.map((figure) => (
            <KPI key={figure.id}>
              <KPI.Header>
                <KPI.Icon status="success">
                  <FaIcon aria-hidden="true" icon={figure.icon} />
                </KPI.Icon>
                <KPI.Title>{t(figure.labelKey)}</KPI.Title>
              </KPI.Header>
              <KPI.Content>
                <KPI.Value
                  maximumFractionDigits={0}
                  style={"isRate" in figure ? "percent" : "decimal"}
                  value={figure.value}
                />
                <KPI.Trend trend="up">{formatPercent(figure.change, locale)}</KPI.Trend>
              </KPI.Content>
              <KPI.Chart color="var(--color-success)" data={figure.trend.map((value) => ({ value }))} height={48} />
              <KPI.Footer className="text-xs text-muted">{t("practiceLanding.mockReportsVsLastWeek")}</KPI.Footer>
            </KPI>
          ))}
        </KPIGroup>
      </Widget.Content>
    </Widget>
  );
}

type BookingState = "pending" | "confirmed" | "present";

const agenda = [
  { id: "consultation", time: "09:00", serviceKey: "practiceLanding.mockAgendaConsultation", providerKey: "practiceLanding.mockProviderOne", initial: "confirmed" },
  { id: "followUp", time: "10:30", serviceKey: "practiceLanding.mockAgendaFollowUp", providerKey: "practiceLanding.mockProviderTwo", initial: "pending" },
  { id: "procedure", time: "13:00", serviceKey: "practiceLanding.mockAgendaProcedure", providerKey: "practiceLanding.mockProviderOne", initial: "pending" },
] as const satisfies readonly { id: string; time: string; serviceKey: MessageKey; providerKey: MessageKey; initial: BookingState }[];

// Reservation status colors from the Web color convention: pending is warning, confirmed success, present accent.
const bookingChips = {
  pending: { color: "warning", labelKey: "practiceLanding.mockPending" },
  confirmed: { color: "success", labelKey: "practiceLanding.mockConfirmed" },
  present: { color: "accent", labelKey: "practiceLanding.mockPresent" },
} as const satisfies Record<BookingState, { color: "warning" | "success" | "accent"; labelKey: MessageKey }>;

/** Today's bookings: confirm a pending one, then check the patient in when they arrive. */
function BookingsMock() {
  const { t, locale } = useLocalizedText();
  const [states, setStates] = useState<Record<string, BookingState>>(() =>
    Object.fromEntries(agenda.map((slot) => [slot.id, slot.initial])),
  );
  const advance = (id: string, next: BookingState) => setStates((current) => ({ ...current, [id]: next }));

  return (
    <ul className="flex flex-col gap-2">
      {agenda.map((slot) => {
        const state = states[slot.id];
        const chip = bookingChips[state];
        return (
          <li key={slot.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-surface p-3">
            <span className="flex flex-col text-sm">
              <span className="font-medium text-foreground">
                {formatTimeOfDay(slot.time, locale)} · {t(slot.serviceKey)}
              </span>
              <span className="text-muted">{t(slot.providerKey)}</span>
            </span>
            <span className="flex items-center gap-2">
              <Chip size="sm" variant="soft" color={chip.color}>{t(chip.labelKey)}</Chip>
              {state === "pending" && (
                <MockAction
                  icon="check"
                  label={t("practiceLanding.mockConfirm")}
                  tooltip={t("practiceLanding.mockConfirmTooltip")}
                  onPress={() => advance(slot.id, "confirmed")}
                />
              )}
              {state === "confirmed" && (
                <MockAction
                  icon="right-to-bracket"
                  label={t("practiceLanding.mockCheckIn")}
                  tooltip={t("practiceLanding.mockCheckInTooltip")}
                  onPress={() => advance(slot.id, "present")}
                />
              )}
            </span>
          </li>
        );
      })}
    </ul>
  );
}

const mocks: Record<LandingFeatureId, () => ReactNode> = {
  assistant: AssistantMock,
  reports: ReportsMock,
  bookings: BookingsMock,
};

export function LandingFeatureMock({ feature }: { feature: LandingFeatureId }) {
  const { t } = useLocalizedText();
  const Mock = mocks[feature];
  return (
    <div className="flex flex-col gap-4">
      <p className="text-xs text-muted">{t("practiceLanding.mockLabel")}</p>
      <Mock />
    </div>
  );
}
