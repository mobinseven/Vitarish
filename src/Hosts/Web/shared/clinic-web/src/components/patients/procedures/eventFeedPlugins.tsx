import type { ComponentType } from "react";
import type { PrescriptionSummaryDto } from "../../../api/generated/medical/model";
import { medicalListPatientPrescriptions } from "../../../api/generated/medical/medical";
import { PrescriptionEventFeedItem } from "./PrescriptionEventFeedItem";

export interface PatientProcedureEventContext {
  patientId: string;
  procedureId: string;
  eventId: string;
}

export interface ProcedureEventPluginItem<TPayload = unknown> {
  id: string;
  createdAt: string;
  pluginId: string;
  payload: TPayload;
}

interface ProcedureEventFeedPlugin<TPayload> {
  id: string;
  load: (
    context: PatientProcedureEventContext,
    signal: AbortSignal,
  ) => Promise<ProcedureEventPluginItem<TPayload>[]>;
  Renderer: ComponentType<{ item: ProcedureEventPluginItem<TPayload> }>;
}

export const prescriptionEventFeedQueryKey = (
  context: Pick<PatientProcedureEventContext, "patientId" | "eventId">,
) => ["clinic", "procedure-event-feed", "prescriptions", context.patientId, context.eventId] as const;

const prescriptionsPlugin: ProcedureEventFeedPlugin<PrescriptionSummaryDto> = {
  id: "medical.prescriptions",
  async load(context, signal) {
    const prescriptions = await medicalListPatientPrescriptions(
      {
        patientId: context.patientId,
        encounterId: context.eventId,
        skip: 0,
        take: 100,
      },
      undefined,
      signal,
    );

    return prescriptions
      .filter(
        (prescription): prescription is PrescriptionSummaryDto & {
          id: string;
          createdAt: string;
        } => Boolean(prescription.id && prescription.createdAt),
      )
      .map((prescription) => ({
        id: `medical.prescription:${prescription.id}`,
        createdAt: prescription.createdAt,
        pluginId: "medical.prescriptions",
        payload: prescription,
      }));
  },
  Renderer: PrescriptionEventFeedItem,
};

export const patientProcedureEventFeedPlugins = [prescriptionsPlugin] as const;
