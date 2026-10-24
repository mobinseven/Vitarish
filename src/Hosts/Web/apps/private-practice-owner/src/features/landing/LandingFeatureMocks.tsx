"use client";

import { useState, type ReactNode } from "react";
import { Button, Chip, Label, ProgressBar, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useLocalizedText, type MessageKey } from "@/localization";

/** The mocks run on fixed sample data and never call the API. */
export const landingFeatureIds = ["patientContext", "appointments", "templates", "checkin", "oversight", "ai"] as const;
export type LandingFeatureId = (typeof landingFeatureIds)[number];

function MockAction({
  icon,
  label,
  tooltip,
  onPress,
}: {
  icon: string;
  label: string;
  tooltip: string;
  onPress: () => void;
}) {
  return (
    <Tooltip>
      <Button size="sm" variant="secondary" onPress={onPress}>
        <FaIcon aria-hidden="true" icon={icon} />
        {label}
      </Button>
      <Tooltip.Content>{tooltip}</Tooltip.Content>
    </Tooltip>
  );
}

function MockRow({ children }: { children: ReactNode }) {
  return <li className="flex items-center justify-between gap-3 rounded-xl bg-surface p-3">{children}</li>;
}

function PatientContextMock() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <span className="flex items-center gap-3 font-semibold text-foreground">
          <FaIcon aria-hidden="true" icon="user" />
          {t("practiceLanding.mockPatientName")}
        </span>
        <Chip size="sm" variant="soft" color="accent">{t("practiceLanding.mockPatientStatus")}</Chip>
      </div>
      <ul className="flex flex-col gap-2 text-sm text-muted">
        <MockRow>{t("practiceLanding.mockPatientNoteOne")}</MockRow>
        <MockRow>{t("practiceLanding.mockPatientNoteTwo")}</MockRow>
      </ul>
      <ProgressBar aria-label={t("practiceLanding.mockPatientProgress")} value={65}>
        <Label>{t("practiceLanding.mockPatientProgress")}</Label>
        <ProgressBar.Output />
        <ProgressBar.Track>
          <ProgressBar.Fill />
        </ProgressBar.Track>
      </ProgressBar>
    </div>
  );
}

const agenda = [
  { id: "consultation", time: "09:00", serviceKey: "practiceLanding.mockAgendaConsultation", providerKey: "practiceLanding.mockProviderOne" },
  { id: "followUp", time: "10:30", serviceKey: "practiceLanding.mockAgendaFollowUp", providerKey: "practiceLanding.mockProviderTwo" },
  { id: "procedure", time: "13:00", serviceKey: "practiceLanding.mockAgendaProcedure", providerKey: "practiceLanding.mockProviderOne" },
] as const satisfies readonly { id: string; time: string; serviceKey: MessageKey; providerKey: MessageKey }[];

function AppointmentsMock() {
  const { t } = useLocalizedText();
  const [confirmed, setConfirmed] = useState<ReadonlySet<string>>(new Set(["consultation"]));

  return (
    <ul className="flex flex-col gap-2">
      {agenda.map((slot) => {
        const isConfirmed = confirmed.has(slot.id);
        return (
          <MockRow key={slot.id}>
            <span className="flex flex-col text-sm">
              <span className="font-medium text-foreground">{slot.time} · {t(slot.serviceKey)}</span>
              <span className="text-muted">{t(slot.providerKey)}</span>
            </span>
            {isConfirmed ? (
              <Chip size="sm" variant="soft" color="success">{t("practiceLanding.mockConfirmed")}</Chip>
            ) : (
              <span className="flex items-center gap-2">
                <Chip size="sm" variant="soft" color="warning">{t("practiceLanding.mockPending")}</Chip>
                <MockAction
                  icon="check"
                  label={t("practiceLanding.mockConfirm")}
                  tooltip={t("practiceLanding.mockConfirmTooltip")}
                  onPress={() => setConfirmed((current) => new Set(current).add(slot.id))}
                />
              </span>
            )}
          </MockRow>
        );
      })}
    </ul>
  );
}

const templateOptions = [
  { id: "prescription", icon: "prescription-bottle-medical", labelKey: "practiceLanding.mockTemplatePrescription", bodyKey: "practiceLanding.mockTemplatePrescriptionBody" },
  { id: "procedure", icon: "notes-medical", labelKey: "practiceLanding.mockTemplateProcedure", bodyKey: "practiceLanding.mockTemplateProcedureBody" },
  { id: "insurance", icon: "file-invoice-dollar", labelKey: "practiceLanding.mockTemplateInsurance", bodyKey: "practiceLanding.mockTemplateInsuranceBody" },
] as const satisfies readonly { id: string; icon: string; labelKey: MessageKey; bodyKey: MessageKey }[];

function TemplatesMock() {
  const { t } = useLocalizedText();
  const [selectedId, setSelectedId] = useState<string>(templateOptions[0].id);
  const selected = templateOptions.find((option) => option.id === selectedId) ?? templateOptions[0];

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap gap-2">
        {templateOptions.map((option) => (
          <Button
            key={option.id}
            aria-pressed={option.id === selectedId}
            size="sm"
            variant={option.id === selectedId ? "primary" : "secondary"}
            onPress={() => setSelectedId(option.id)}
          >
            <FaIcon aria-hidden="true" icon={option.icon} />
            {t(option.labelKey)}
          </Button>
        ))}
      </div>
      <p aria-live="polite" className="rounded-xl bg-surface p-4 text-sm leading-6 text-muted">
        {t(selected.bodyKey)}
      </p>
    </div>
  );
}

const expectedPatients = ["practiceLanding.mockPatientOne", "practiceLanding.mockPatientTwo"] as const satisfies readonly MessageKey[];

function CheckinMock() {
  const { t } = useLocalizedText();
  const [arrived, setArrived] = useState<readonly MessageKey[]>([]);
  const expected = expectedPatients.filter((key) => !arrived.includes(key));

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <section aria-label={t("practiceLanding.mockExpected")} className="flex flex-col gap-2">
        <h4 className="text-xs font-semibold uppercase tracking-wide text-muted">{t("practiceLanding.mockExpected")}</h4>
        <ul className="flex flex-col gap-2">
          {expected.map((key) => (
            <MockRow key={key}>
              <span className="text-sm font-medium text-foreground">{t(key)}</span>
              <MockAction
                icon="right-to-bracket"
                label={t("practiceLanding.mockCheckIn")}
                tooltip={t("practiceLanding.mockCheckInTooltip")}
                onPress={() => setArrived((current) => [...current, key])}
              />
            </MockRow>
          ))}
        </ul>
      </section>
      <section aria-label={t("practiceLanding.mockArrived")} className="flex flex-col gap-2">
        <h4 className="text-xs font-semibold uppercase tracking-wide text-muted">{t("practiceLanding.mockArrived")}</h4>
        <ul className="flex flex-col gap-2">
          {arrived.map((key) => (
            <MockRow key={key}>
              <span className="text-sm font-medium text-foreground">{t(key)}</span>
              <Chip size="sm" variant="soft" color="success">{t("practiceLanding.mockArrived")}</Chip>
            </MockRow>
          ))}
        </ul>
      </section>
    </div>
  );
}

const oversightFigures = [
  { id: "patients", value: "128", labelKey: "practiceLanding.mockOversightPatients" },
  { id: "today", value: "18", labelKey: "practiceLanding.mockOversightToday" },
  { id: "attention", value: "3", labelKey: "practiceLanding.mockOversightAttention" },
] as const satisfies readonly { id: string; value: string; labelKey: MessageKey }[];

function OversightMock() {
  const { t } = useLocalizedText();
  return (
    <dl className="grid gap-3 sm:grid-cols-3">
      {oversightFigures.map((figure) => (
        <div key={figure.id} className="flex flex-col gap-1 rounded-xl bg-surface p-4">
          <dd className="text-3xl font-semibold tabular-nums text-foreground">{figure.value}</dd>
          <dt className="text-sm text-muted">{t(figure.labelKey)}</dt>
        </div>
      ))}
    </dl>
  );
}

type SuggestionState = "open" | "accepted" | "dismissed";

function AiGuidanceMock() {
  const { t } = useLocalizedText();
  const [state, setState] = useState<SuggestionState>("open");

  if (state !== "open") {
    return (
      <div aria-live="polite" className="flex flex-col items-start gap-3">
        <p className="text-sm text-muted">
          {state === "accepted" ? t("practiceLanding.mockAiAccepted") : t("practiceLanding.mockAiDismissed")}
        </p>
        <MockAction
          icon="rotate-left"
          label={t("practiceLanding.mockAiReset")}
          tooltip={t("practiceLanding.mockAiResetTooltip")}
          onPress={() => setState("open")}
        />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="flex gap-3 rounded-xl bg-surface p-4 text-sm leading-6 text-foreground">
        <FaIcon aria-hidden="true" icon="wand-magic-sparkles" className="mt-1 shrink-0 text-accent" />
        {t("practiceLanding.mockAiSummary")}
      </p>
      <div className="flex flex-wrap gap-2">
        <MockAction
          icon="check"
          label={t("practiceLanding.mockAiAccept")}
          tooltip={t("practiceLanding.mockAiAcceptTooltip")}
          onPress={() => setState("accepted")}
        />
        <MockAction
          icon="xmark"
          label={t("practiceLanding.mockAiDismiss")}
          tooltip={t("practiceLanding.mockAiDismissTooltip")}
          onPress={() => setState("dismissed")}
        />
      </div>
    </div>
  );
}

const mocks: Record<LandingFeatureId, () => ReactNode> = {
  patientContext: PatientContextMock,
  appointments: AppointmentsMock,
  templates: TemplatesMock,
  checkin: CheckinMock,
  oversight: OversightMock,
  ai: AiGuidanceMock,
};

export function LandingFeatureMock({ feature }: { feature: LandingFeatureId }) {
  const { t } = useLocalizedText();
  const Mock = mocks[feature];
  return (
    <div className="flex flex-col gap-3">
      <p className="text-xs text-muted">{t("practiceLanding.mockLabel")}</p>
      <Mock />
    </div>
  );
}
