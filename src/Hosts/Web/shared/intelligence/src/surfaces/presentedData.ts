import { formatDate, formatDateTime, formatNumber, type SupportedLocale } from "@vitarish/localization";

/** How a presented value is formatted; mirrors the server's presentable field types (ADR-0022 decision 8). */
export type PresentedFieldType = "Text" | "Date" | "DateTime" | "Number" | "Status";

export interface PresentedField {
  type: PresentedFieldType;
  value: unknown;
}

/** One record the server filled into a surface's data model from a read of this turn. */
export interface PresentedRecord {
  kind: string;
  reference: string;
  title: PresentedField;
  fields: Record<string, PresentedField>;
}

/** The value a surface binds a component's `result` to. */
export interface PresentedResult {
  kind: string;
  isList: boolean;
  records: PresentedRecord[];
  hasMore: boolean;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function asField(value: unknown): PresentedField | null {
  return isRecord(value) && typeof value.type === "string" ? { type: value.type as PresentedFieldType, value: value.value } : null;
}

/** Reads a bound result defensively; anything malformed renders as nothing rather than as wrong data. */
export function asPresentedResult(value: unknown): PresentedResult | null {
  if (!isRecord(value) || typeof value.kind !== "string" || !Array.isArray(value.records)) return null;
  const records: PresentedRecord[] = [];
  for (const item of value.records) {
    if (!isRecord(item) || typeof item.kind !== "string" || typeof item.reference !== "string") continue;
    const title = asField(item.title);
    if (!title) continue;
    const fields: Record<string, PresentedField> = {};
    if (isRecord(item.fields)) {
      for (const [name, field] of Object.entries(item.fields)) {
        const parsed = asField(field);
        if (parsed) fields[name] = parsed;
      }
    }
    records.push({ kind: item.kind, reference: item.reference, title, fields });
  }
  return { kind: value.kind, isList: value.isList === true, records, hasMore: value.hasMore === true };
}

/** Formats a presented value for the viewer's locale and calendar; empty values return null. */
export function formatPresentedValue(field: PresentedField | undefined, locale: SupportedLocale, status?: (value: string) => string): string | null {
  if (!field || field.value === null || field.value === undefined || field.value === "") return null;
  switch (field.type) {
    case "Date":
      return typeof field.value === "string" ? formatDate(field.value, locale) : null;
    case "DateTime":
      return typeof field.value === "string" ? formatDateTime(field.value, locale) : null;
    case "Number":
      return typeof field.value === "number" ? formatNumber(field.value, locale) : null;
    case "Status":
      return typeof field.value === "string" ? (status ? status(field.value) : field.value) : null;
    default:
      return typeof field.value === "string" ? field.value : String(field.value);
  }
}
