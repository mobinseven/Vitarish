"use client";

import { Button, Chip, Input, Label, ListBox, Select, TextArea, TextField, Tooltip } from "@heroui/react";
import { ApiError } from "@vitarish/api-client";
import { useLocalizedText } from "@vitarish/localization";
import type { ComponentProps, ReactNode } from "react";
import type { ContentEntryStatus } from "./contentsStudioApi";

type StudioButtonVariant = "primary" | "tertiary" | "outline" | "ghost";

/**
 * A labelled action with its icon and tooltip; on narrow screens it shrinks to the icon and the tooltip names it.
 * Generic actions stay neutral; pass `primary` only for a section's lead or commit action.
 */
export function StudioButton({ label, icon, onPress, disabled = false, variant = "tertiary", className }: { label: string; icon: string; onPress: () => void; disabled?: boolean; variant?: StudioButtonVariant; className?: string }) {
  return <Tooltip><Button className={`text-base ${className ?? ""}`} variant={variant} isDisabled={disabled} onPress={onPress}><i className={`fa-light ${icon}`} aria-hidden="true" /><span className="max-sm:sr-only">{label}</span></Button><Tooltip.Content>{label}</Tooltip.Content></Tooltip>;
}

/** Renders a section surface as a section landmark. */
export function renderSection(props: ComponentProps<"div">) {
  return <section {...props} />;
}

export function StudioInput({ label, value, onChange, type, readOnly, error }: { label: string; value: string; onChange: (value: string) => void; type?: string; readOnly?: boolean; error?: string }) {
  return <TextField className="flex flex-col gap-1"><Label className="text-base">{label}</Label><Input className="text-base" variant="secondary" type={type} value={value} readOnly={readOnly} onChange={(event) => onChange(event.target.value)} aria-invalid={Boolean(error)} />{error && <p className="text-base text-danger" role="alert">{error}</p>}</TextField>;
}

export function StudioTextArea({ label, value, onChange, error, readOnly }: { label: string; value: string; onChange: (value: string) => void; error?: string; readOnly?: boolean }) {
  return <TextField className="flex flex-col gap-1"><Label className="text-base">{label}</Label><TextArea className="text-base" variant="secondary" value={value} readOnly={readOnly} onChange={(event) => onChange(event.target.value)} aria-invalid={Boolean(error)} />{error && <p className="text-base text-danger" role="alert">{error}</p>}</TextField>;
}

// React Aria treats an empty key as "nothing selected"; an option whose id is "" (such as "All") needs a stand-in key.
const emptyOptionKey = "studio-empty-option";
const toKey = (id: string) => id === "" ? emptyOptionKey : id;
const fromKey = (key: string) => key === emptyOptionKey ? "" : key;

export function StudioSelect({ label, value, options, onChange, className }: { label: string; value: string; options: readonly { id: string; label: string }[]; onChange: (value: string) => void; className?: string }) {
  const selected = options.some((option) => option.id === value) ? toKey(value) : null;
  return <div className={`flex flex-col gap-1 ${className ?? ""}`}><Label className="text-base">{label}</Label><Select aria-label={label} variant="secondary" value={selected} onChange={(next) => onChange(next === null ? "" : fromKey(String(next)))}><Select.Trigger className="text-base"><Select.Value /><Select.Indicator /></Select.Trigger><Select.Popover><ListBox>{options.map((option) => <ListBox.Item className="text-base" key={toKey(option.id)} id={toKey(option.id)} textValue={option.label}>{option.label}<ListBox.ItemIndicator /></ListBox.Item>)}</ListBox></Select.Popover></Select></div>;
}

const statusColors: Record<ContentEntryStatus, "default" | "success" | "warning" | "accent"> = {
  Draft: "default",
  Scheduled: "warning",
  Published: "success",
  Archived: "default",
};

export function EntryStatusChip({ status }: { status: ContentEntryStatus }) {
  const { t } = useLocalizedText();
  return <Chip color={statusColors[status]} variant="soft">{t(`contentsStudio.${status}`)}</Chip>;
}

export function StudioState({ children, error = false }: { children: ReactNode; error?: boolean }) {
  return <p className={`text-base ${error ? "text-danger" : "text-muted"}`} role={error ? "alert" : "status"}>{children}</p>;
}

export function isConflict(error: unknown): boolean {
  return error instanceof ApiError && error.status === 409;
}

export function StudioFailure({ error, conflict = false }: { error: unknown; conflict?: boolean }) {
  const { t } = useLocalizedText();
  const status = error instanceof ApiError ? error.status : undefined;
  const message = status === 401 ? "unauthorized" : status === 403 ? "forbidden" : status === 409 && conflict ? "conflict" : status === 503 || status === 502 || status === 504 ? "unavailable" : "error";
  return <StudioState error>{t(`contentsStudio.${message}`)}</StudioState>;
}
