/**
 * Consumption values a provider requires for a drug, chosen from lists that depend on the drug's dosage form.
 * They travel as prescription item metadata; the keys are the contract the provider adapter reads.
 */
export const drugConsumptionMetadataKeys = {
  amountId: "drugAmntId",
  instructionId: "drugInstId",
  routeCode: "doseCode",
  methodId: "methodUsageId",
} as const;

/** Metadata key under which the synced drug catalog carries a drug's dosage form. */
export const dosageFormMetadataKey = "formCode";

/**
 * Numeric value of the Medical domain's drug item category. The API passes item categories through
 * numerically between catalog search and item creation, so this is the value both calls use.
 */
export const drugItemCategory = 1;

export interface DrugConsumptionSelection {
  amountId: string;
  instructionId: string;
  routeCode?: string;
  methodId?: string;
}

/** Ids are numeric in the amount, time and method lists; the route is a code string. */
export function buildDrugConsumptionMetadata(
  selection: DrugConsumptionSelection,
): string {
  const metadata: Record<string, string | number> = {
    [drugConsumptionMetadataKeys.amountId]: Number(selection.amountId),
    [drugConsumptionMetadataKeys.instructionId]: Number(
      selection.instructionId,
    ),
  };
  if (selection.routeCode) {
    metadata[drugConsumptionMetadataKeys.routeCode] = selection.routeCode;
  }
  if (selection.methodId) {
    metadata[drugConsumptionMetadataKeys.methodId] = Number(selection.methodId);
  }
  return JSON.stringify(metadata);
}
