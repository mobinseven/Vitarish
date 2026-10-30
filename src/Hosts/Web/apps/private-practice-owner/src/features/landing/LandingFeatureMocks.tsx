"use client";

import { useState, type ReactNode } from "react";
import { Alert, Button, Chip, Surface, Tooltip } from "@heroui/react";
import { BarChart } from "@heroui-pro/react/bar-chart";
import { ChatMessage, FaIcon } from "@vitarish/ui";
import { formatNumber, formatPercent, formatTimeOfDay, useLocalizedText, type MessageKey } from "@/localization";
import styles from "./landing-demos.module.css";

/** The mocks run on fixed sample data and never call the API. */
export const landingFeatureIds = ["assistant", "reports", "bookings"] as const;
export type LandingFeatureId = (typeof landingFeatureIds)[number];

function MockAction({
  icon,
  label,
  tooltip,
  onPress,
  className,
  variant = "tertiary",
}: {
  icon: string;
  label: string;
  tooltip: string;
  onPress: () => void;
  className?: string;
  variant?: "primary" | "tertiary" | "outline";
}) {
  return (
    <Tooltip>
      <Button className={className} size="sm" variant={variant} onPress={onPress}>
        <FaIcon aria-hidden="true" icon={icon} />
        {label}
      </Button>
      <Tooltip.Content>{tooltip}</Tooltip.Content>
    </Tooltip>
  );
}

// A local illustration of the existing prepare -> clinician confirmation workflow.
// Team, equipment and reminder defaults come from the surgery type in the real app.
const surgeryExample = { time: "09:00", durationMinutes: 90, reminderHours: [24, 2] } as const;
const surgeryPreparation = [
  { icon: "calendar-check", labelKey: "practiceLanding.mockSurgeryConflicts", detailKey: "practiceLanding.mockSurgeryConflictsDetail" },
  { icon: "user-group", labelKey: "practiceLanding.mockSurgeryTeam", detailKey: "practiceLanding.mockSurgeryTeamDetail" },
  { icon: "briefcase-medical", labelKey: "practiceLanding.mockSurgeryEquipment", detailKey: "practiceLanding.mockSurgeryEquipmentDetail" },
  { icon: "bell", labelKey: "practiceLanding.mockSurgeryReminders", detailKey: "practiceLanding.mockSurgeryRemindersDetail" },
] as const satisfies readonly { icon: string; labelKey: MessageKey; detailKey: MessageKey }[];

/** The surgeon reviews the assembled surgery plan before confirming it. */
function AssistantMock() {
  const { t, locale } = useLocalizedText();
  const [confirmed, setConfirmed] = useState(false);
  const time = formatTimeOfDay(surgeryExample.time, locale);
  const assistant = { avatarAlt: t("practiceLanding.mockAssistantName"), avatarFallback: t("practiceLanding.mockAssistantAvatar") };

  return (
    <div>
      <div className={styles.demoHeader}>
        <div className={styles.headerIdentity}>
          <FaIcon aria-hidden="true" icon="message-bot" className={styles.headerIcon} />
          <div>
            <p className={styles.headerEyebrow}>{t("practiceLanding.mockSurgeryEyebrow")}</p>
            <h3>{t("practiceLanding.mockAssistantName")}</h3>
          </div>
        </div>
        <Chip color={confirmed ? "success" : "warning"} size="sm" variant="soft">
          <FaIcon aria-hidden="true" icon={confirmed ? "check" : "clipboard-check"} />
          {t(confirmed ? "practiceLanding.mockSurgerySaved" : "practiceLanding.mockSurgeryReview")}
        </Chip>
      </div>
      <div className={styles.conversation}>
        <ChatMessage kind="question">{t("practiceLanding.mockSurgeryRequest", { time })}</ChatMessage>
        <ChatMessage {...assistant} kind="answer">
          {t("practiceLanding.mockSurgeryPrepared")}
        </ChatMessage>
        <Surface className={styles.surgeryPlan} variant="secondary">
          <div className={styles.surgeryHeading}>
            <div>
              <p className={styles.surgeryPatient}>{t("practiceLanding.mockSurgeryPatient")}</p>
              <h4>{t("practiceLanding.mockSurgeryProcedure")}</h4>
            </div>
            <FaIcon aria-hidden="true" icon="clipboard-medical" className={styles.planIcon} />
          </div>
          <div className={styles.surgerySchedule}>
            <span><FaIcon aria-hidden="true" icon="calendar" />{t("practiceLanding.mockSurgeryDay", { time })}</span>
            <span><FaIcon aria-hidden="true" icon="clock" />{t("practiceLanding.mockSurgeryDuration", { minutes: formatNumber(surgeryExample.durationMinutes, locale) })}</span>
          </div>
          <dl className={styles.preparationList}>
            {surgeryPreparation.map((step) => (
              <div key={step.labelKey} className={styles.preparationRow}>
                <FaIcon aria-hidden="true" icon={step.icon} className={styles.preparationIcon} />
                <div>
                  <dt>{t(step.labelKey)}</dt>
                  <dd>{t(step.detailKey, { first: formatNumber(surgeryExample.reminderHours[0], locale), second: formatNumber(surgeryExample.reminderHours[1], locale) })}</dd>
                </div>
                <FaIcon aria-hidden="true" icon="circle-check" className={styles.preparedCheck} />
              </div>
            ))}
          </dl>
          {!confirmed && (
            <MockAction
              className={styles.confirmSurgery}
              icon="calendar-plus"
              label={t("practiceLanding.mockSurgeryConfirm")}
              tooltip={t("practiceLanding.mockSurgeryConfirmTooltip")}
              variant="primary"
              onPress={() => setConfirmed(true)}
            />
          )}
        </Surface>
        <div aria-live="polite">
          {confirmed && (
            <Alert className={styles.confirmation} status="success">
              <Alert.Indicator><FaIcon aria-hidden="true" icon="calendar-check" /></Alert.Indicator>
              <Alert.Content>
                <Alert.Title>{t("practiceLanding.mockSurgeryDone")}</Alert.Title>
                <Alert.Description>{t("practiceLanding.mockSurgeryDoneDetail")}</Alert.Description>
                <div className={styles.restart}>
                  <MockAction icon="rotate-left" label={t("practiceLanding.mockSurgeryRestart")} tooltip={t("practiceLanding.mockSurgeryRestartTooltip")} onPress={() => setConfirmed(false)} />
                </div>
              </Alert.Content>
            </Alert>
          )}
        </div>
      </div>
    </div>
  );
}

// Seven sample weeks. Each headline is the last point in its series.
const reportFigures = [
  { id: "visits", icon: "stethoscope", labelKey: "practiceLanding.mockReportsVisits", trend: [52, 58, 55, 63, 68, 72, 86] },
  { id: "newPatients", icon: "user-plus", labelKey: "practiceLanding.mockReportsNewPatients", trend: [6, 8, 7, 9, 11, 12, 14] },
  { id: "returning", icon: "rotate", labelKey: "practiceLanding.mockReportsReturning", trend: [54, 57, 59, 58, 62, 65, 68], isRate: true },
] as const satisfies readonly {
  id: string;
  icon: string;
  labelKey: MessageKey;
  trend: readonly number[];
  isRate?: boolean;
}[];

function ReportsMock() {
  const { t, locale } = useLocalizedText();
  const [selectedId, setSelectedId] = useState<(typeof reportFigures)[number]["id"]>(reportFigures[0].id);
  const selected = reportFigures.find((figure) => figure.id === selectedId)!;
  const formatValue = (figure: (typeof reportFigures)[number], value: number) => "isRate" in figure
    ? formatPercent(value / 100, locale)
    : formatNumber(value, locale);
  const previous = selected.trend.at(-2)!;
  const change = (selected.trend.at(-1)! - previous) / previous;
  const series = selected.trend.map((value, index) => ({
    period: t("practiceLanding.mockReportsWeek", { number: formatNumber(index + 1, locale) }),
    value,
  }));

  return (
    <div>
      <div className={styles.demoHeader}>
        <div className={styles.headerIdentity}>
          <FaIcon aria-hidden="true" icon="chart-column" className={styles.headerIcon} />
          <div>
            <p className={styles.headerEyebrow}>{t("practiceLanding.mockReportsEyebrow")}</p>
            <h3>{t("practiceLanding.mockReportsTitle")}</h3>
          </div>
        </div>
        <span className={styles.reportPeriod}>{t("practiceLanding.mockReportsHistory")}</span>
      </div>
      <div className={styles.reportBody}>
        <div className={styles.metricChoices}>
          {reportFigures.map((figure) => (
            <Tooltip key={figure.id}>
              <Button className={styles.metricChoice} variant={figure.id === selectedId ? "secondary" : "tertiary"} aria-pressed={figure.id === selectedId} onPress={() => setSelectedId(figure.id)}>
                <span className={styles.metricLabel}><FaIcon aria-hidden="true" icon={figure.icon} />{t(figure.labelKey)}</span>
                <span className={styles.metricValue}>{formatValue(figure, figure.trend.at(-1)!)}</span>
              </Button>
              <Tooltip.Content>{t("practiceLanding.mockReportsSelect", { metric: t(figure.labelKey) })}</Tooltip.Content>
            </Tooltip>
          ))}
        </div>
        <div className={styles.reportSummary} aria-live="polite">
          <div>
            <p className={styles.reportMetric}>{t(selected.labelKey)}</p>
            <p className={styles.reportHeadline}>{formatValue(selected, selected.trend.at(-1)!)}</p>
          </div>
          <div className={styles.reportTrend}>
            <Chip color="success" variant="soft"><FaIcon aria-hidden="true" icon="arrow-trend-up" />{formatPercent(change, locale)}</Chip>
            <span>{t("practiceLanding.mockReportsVsLastWeek")}</span>
          </div>
        </div>
        <div className={styles.reportChart} role="img" aria-label={t("practiceLanding.mockReportsChart", { metric: t(selected.labelKey) })}>
          <BarChart data={series} height={210}>
            <BarChart.Grid vertical={false} />
            <BarChart.XAxis dataKey="period" tickMargin={10} interval={0} />
            <BarChart.YAxis width={42} tickFormatter={(value: number) => formatValue(selected, value)} />
            <BarChart.Bar dataKey="value" name={t(selected.labelKey)} fill="var(--accent)" radius={[8, 8, 0, 0]} maxBarSize={44} isAnimationActive={false} />
            <BarChart.Tooltip content={<BarChart.TooltipContent />} formatter={(value) => formatValue(selected, Number(value))} />
          </BarChart>
        </div>
        <dl className="sr-only">{series.map((point) => <div key={point.period}><dt>{point.period}</dt><dd>{formatValue(selected, point.value)}</dd></div>)}</dl>
      </div>
    </div>
  );
}

type BookingState = "pending" | "confirmed" | "present";

const agenda = [
  { id: "consultation", time: "09:00", serviceKey: "practiceLanding.mockAgendaConsultation", providerKey: "practiceLanding.mockProviderOne", initial: "confirmed" },
  { id: "followUp", time: "10:30", serviceKey: "practiceLanding.mockAgendaFollowUp", providerKey: "practiceLanding.mockProviderTwo", initial: "pending" },
  { id: "procedure", time: "13:00", serviceKey: "practiceLanding.mockAgendaProcedure", providerKey: "practiceLanding.mockProviderOne", initial: "pending" },
] as const satisfies readonly { id: string; time: string; serviceKey: MessageKey; providerKey: MessageKey; initial: BookingState }[];

// Reservation status colors: pending is warning, confirmed success, present accent.
const bookingChips = {
  pending: { color: "warning", labelKey: "practiceLanding.mockPending", className: styles.pending, icon: "clock" },
  confirmed: { color: "success", labelKey: "practiceLanding.mockConfirmed", className: styles.confirmed, icon: "circle-check" },
  present: { color: "accent", labelKey: "practiceLanding.mockPresent", className: styles.present, icon: "user-check" },
} as const satisfies Record<BookingState, { color: "warning" | "success" | "accent"; labelKey: MessageKey; className: string; icon: string }>;

/** Confirm a sample appointment, then check the patient in. */
function BookingsMock() {
  const { t, locale } = useLocalizedText();
  const [states, setStates] = useState<Record<string, BookingState>>(() => Object.fromEntries(agenda.map((slot) => [slot.id, slot.initial])));
  const advance = (id: string, next: BookingState) => setStates((current) => ({ ...current, [id]: next }));

  return (
    <div>
      <div className={styles.demoHeader}>
        <div className={styles.headerIdentity}>
          <FaIcon aria-hidden="true" icon="calendar-days" className={styles.headerIcon} />
          <div>
            <p className={styles.headerEyebrow}>{t("practiceLanding.mockAgendaEyebrow")}</p>
            <h3>{t("practiceLanding.mockAgendaTitle")}</h3>
          </div>
        </div>
        <span className={styles.agendaCount}>{formatNumber(agenda.length, locale)}</span>
      </div>
      <div className={styles.agendaBody}>
        <div aria-hidden="true" className={styles.dayProgress}>
          {agenda.map((slot) => <span key={slot.id} className={bookingChips[states[slot.id]].className} />)}
        </div>
        <ul className={styles.agenda} aria-live="polite">
          {agenda.map((slot) => {
            const state = states[slot.id];
            const chip = bookingChips[state];
            return (
              <li key={slot.id} className={`${styles.agendaItem} ${chip.className}`}>
                <span className={styles.agendaTime}>{formatTimeOfDay(slot.time, locale)}</span>
                <span className={styles.agendaDetails}>
                  <span>{t(slot.serviceKey)}</span>
                  <span className={styles.provider}><FaIcon aria-hidden="true" icon="user-doctor" />{t(slot.providerKey)}</span>
                </span>
                <span className={styles.agendaActions}>
                  <Chip size="sm" variant="soft" color={chip.color}><FaIcon aria-hidden="true" icon={chip.icon} />{t(chip.labelKey)}</Chip>
                  {state === "pending" && <MockAction icon="check" label={t("practiceLanding.mockConfirm")} tooltip={t("practiceLanding.mockConfirmTooltip")} onPress={() => advance(slot.id, "confirmed")} />}
                  {state === "confirmed" && <MockAction icon="right-to-bracket" label={t("practiceLanding.mockCheckIn")} tooltip={t("practiceLanding.mockCheckInTooltip")} onPress={() => advance(slot.id, "present")} />}
                </span>
              </li>
            );
          })}
        </ul>
        <p className={styles.teamNote}><FaIcon aria-hidden="true" icon="users" />{t("practiceLanding.mockAgendaTeam")}</p>
      </div>
    </div>
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
    <div className={styles.demo}>
      <Mock />
      <p className={styles.sampleLabel}><FaIcon aria-hidden="true" icon="flask" />{t("practiceLanding.mockLabel")}</p>
    </div>
  );
}
