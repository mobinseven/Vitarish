import {
  formatDate,
  formatRelativeTime,
  type SupportedLocale,
} from "../../../localization/index";
import type {
  PatientSortBy,
  PatientViewDto,
} from "../../../models/patients/patientSearchModels";

export interface PatientSortValueContext {
  locale: SupportedLocale;
  now?: Date;
}

export interface PatientSortValueDefinition {
  labelKey: string;
  value: (
    patient: PatientViewDto,
    context: PatientSortValueContext,
  ) => string | null;
}

export interface PatientSortValue {
  labelKey: string;
  value: string;
}

const identificationCodeLabelKey = "clinic.patient.fields.identificationCode";
const nextSurgeryLabelKey = "clinic.patient.sortValue.nextSurgery";

const dateWithRelativeSeparator = " · ";

const relativeTimeUnits: ReadonlyArray<
  readonly [Intl.RelativeTimeFormatUnit, number]
> = [
  ["year", 365 * 24 * 60 * 60 * 1000],
  ["month", 30 * 24 * 60 * 60 * 1000],
  ["week", 7 * 24 * 60 * 60 * 1000],
  ["day", 24 * 60 * 60 * 1000],
  ["hour", 60 * 60 * 1000],
  ["minute", 60 * 1000],
];

function relativePhrase(value: string, context: PatientSortValueContext): string {
  const now = context.now ?? new Date();
  const difference = new Date(value).getTime() - now.getTime();
  const magnitude = Math.abs(difference);
  const match = relativeTimeUnits.find(([, unitMs]) => magnitude >= unitMs);
  if (!match) {
    return formatRelativeTime(
      Math.round(difference / 1000),
      "second",
      context.locale,
    );
  }
  const [unit, unitMs] = match;
  return formatRelativeTime(Math.round(difference / unitMs), unit, context.locale);
}

function dateValue(
  value: string | null | undefined,
  context: PatientSortValueContext,
): string | null {
  return value ? formatDate(value, context.locale) : null;
}

function dateWithRelativeValue(
  value: string | null | undefined,
  context: PatientSortValueContext,
): string | null {
  if (!value) {
    return null;
  }
  return `${formatDate(value, context.locale)}${dateWithRelativeSeparator}${relativePhrase(value, context)}`;
}

export const patientSortValues: Record<
  PatientSortBy,
  PatientSortValueDefinition
> = {
  Name: {
    labelKey: identificationCodeLabelKey,
    value: (patient) => patient.identificationCode ?? null,
  },
  Template: {
    labelKey: identificationCodeLabelKey,
    value: (patient) => patient.identificationCode ?? null,
  },
  IdentificationCode: {
    labelKey: "clinic.patient.sortValue.identificationCode",
    value: (patient) => patient.identificationCode ?? null,
  },
  DateOfBirth: {
    labelKey: "clinic.patient.sortValue.dateOfBirth",
    value: (patient, context) => dateValue(patient.dateOfBirth, context),
  },
  DateOfSurgery: {
    labelKey: nextSurgeryLabelKey,
    value: (patient, context) =>
      dateWithRelativeValue(patient.nextSurgeryAt, context),
  },
  CreatedAt: {
    labelKey: "clinic.patient.sortValue.createdAt",
    value: (patient, context) => dateValue(patient.createdAt, context),
  },
  UpdatedAt: {
    labelKey: "clinic.patient.sortValue.updatedAt",
    value: (patient, context) => dateWithRelativeValue(patient.updatedAt, context),
  },
  LastSurgery: {
    labelKey: "clinic.patient.sortValue.lastSurgery",
    value: (patient, context) =>
      dateWithRelativeValue(patient.lastSurgeryAt, context),
  },
  NextSurgery: {
    labelKey: nextSurgeryLabelKey,
    value: (patient, context) =>
      dateWithRelativeValue(patient.nextSurgeryAt, context),
  },
};

export function resolvePatientSortValue(
  sortBy: PatientSortBy | undefined,
  patient: PatientViewDto,
  context: PatientSortValueContext,
): PatientSortValue | null {
  const definition = patientSortValues[sortBy ?? "Name"];
  const value = definition.value(patient, context);
  return value ? { labelKey: definition.labelKey, value } : null;
}
