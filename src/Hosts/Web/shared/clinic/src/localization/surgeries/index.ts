import { surgeryAssistantMessages } from "./assistant";
import { surgeryCalendarMessages } from "./calendar";
import { surgeryCommonMessages } from "./common";
import { surgeryDetailMessages } from "./detail";
import { surgeryPatientPanelMessages } from "./patientPanel";
import { surgeryPlanMessages } from "./plan";
import { surgeryTypeMessages } from "./types";

export const surgeriesEnMessages = {
  ...surgeryCommonMessages.en,
  calendar: surgeryCalendarMessages.en,
  plan: surgeryPlanMessages.en,
  detail: surgeryDetailMessages.en,
  types: surgeryTypeMessages.en,
  patientPanel: surgeryPatientPanelMessages.en,
  assistant: surgeryAssistantMessages.en,
} as const;

export const surgeriesFaMessages = {
  ...surgeryCommonMessages.fa,
  calendar: surgeryCalendarMessages.fa,
  plan: surgeryPlanMessages.fa,
  detail: surgeryDetailMessages.fa,
  types: surgeryTypeMessages.fa,
  patientPanel: surgeryPatientPanelMessages.fa,
  assistant: surgeryAssistantMessages.fa,
} as const;
