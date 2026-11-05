"use client";

import { Button, Input, Label, ListBox, Select, TextArea, TextField, Tooltip } from "@heroui/react";
import { ApiError } from "@vitarish/api-client";
import { useLocalizedText } from "@vitarish/localization";
import type { ReactNode } from "react";

export function StudioButton({ label, icon, onPress, disabled = false }: { label: string; icon: string; onPress: () => void; disabled?: boolean }) {
  return <Tooltip><Button className="text-base" variant="secondary" aria-label={label} isDisabled={disabled} onPress={onPress}><i className={`fa-light ${icon}`} aria-hidden="true" />{label}</Button><Tooltip.Content>{label}</Tooltip.Content></Tooltip>;
}

export function StudioInput({ label, value, onChange, type, readOnly, error }: { label: string; value: string; onChange: (value: string) => void; type?: string; readOnly?: boolean; error?: string }) {
  return <TextField><Label className="text-base">{label}</Label><Input className="text-base" variant="secondary" type={type} value={value} readOnly={readOnly} onChange={(event) => onChange(event.target.value)} aria-invalid={Boolean(error)} />{error && <p role="alert">{error}</p>}</TextField>;
}

export function StudioTextArea({ label, value, onChange, error, readOnly }: { label: string; value: string; onChange: (value: string) => void; error?: string; readOnly?: boolean }) {
  return <TextField><Label className="text-base">{label}</Label><TextArea className="text-base" variant="secondary" value={value} readOnly={readOnly} onChange={(event) => onChange(event.target.value)} aria-invalid={Boolean(error)} />{error && <p role="alert">{error}</p>}</TextField>;
}

export function StudioSelect({ label, value, options, onChange }: { label: string; value: string; options: readonly { id: string; label: string }[]; onChange: (value: string) => void }) {
  return <div className="flex flex-col gap-1"><Label className="text-base">{label}</Label><Select aria-label={label} variant="secondary" value={value || null} onChange={(next) => onChange(next === null ? "" : String(next))}><Select.Trigger className="text-base"><Select.Value /><Select.Indicator /></Select.Trigger><Select.Popover><ListBox>{options.map((option) => <ListBox.Item className="text-base" key={option.id} id={option.id} textValue={option.label}>{option.label}<ListBox.ItemIndicator /></ListBox.Item>)}</ListBox></Select.Popover></Select></div>;
}

export function StudioState({ children, error = false }: { children: ReactNode; error?: boolean }) {
  return <p role={error ? "alert" : "status"}>{children}</p>;
}

export function StudioFailure({ error, conflict = false }: { error: unknown; conflict?: boolean }) {
  const { t } = useLocalizedText();
  const status = error instanceof ApiError ? error.status : undefined;
  const message = status === 401 ? "unauthorized" : status === 403 ? "forbidden" : status === 409 && conflict ? "conflict" : status === 503 || status === 502 || status === 504 ? "unavailable" : "error";
  return <StudioState error>{t(`contentsStudio.${message}`)}</StudioState>;
}
