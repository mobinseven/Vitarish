"use client";

import { useState } from "react";
import { Button, Input, Label, TextField, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../localization";
import { asRecord } from "./actionPayload";

type Primitive = string | number | boolean;

/** The arguments a revisable appointment-management proposal carries: one operation and its payload. */
interface ManageArguments {
  operation: string;
  payload: Record<string, unknown>;
}

function readArguments(reviewPayload: unknown): ManageArguments {
  const root = asRecord(reviewPayload);
  return { operation: typeof root.operation === "string" ? root.operation : "", payload: asRecord(root.payload) };
}

function primitiveFields(payload: Record<string, unknown>): [string, Primitive][] {
  return Object.entries(payload).filter((entry): entry is [string, Primitive] =>
    ["string", "number", "boolean"].includes(typeof entry[1]));
}

export interface ManageActionCardProps {
  reviewPayload: unknown;
  /** Present while the proposal can still be edited; resolves when the edit was saved as a new proposal. */
  onRevise?: (argumentsJson: Record<string, unknown>) => Promise<void>;
}

/** An appointment-management proposal: what it changes, and an edit form that keeps the same operation. */
export function ManageActionCard({ reviewPayload, onRevise }: ManageActionCardProps) {
  const { t } = useLocalizedText();
  const { operation, payload } = readArguments(reviewPayload);
  const fields = primitiveFields(payload);
  const [isEditing, setEditing] = useState(false);
  const [draft, setDraft] = useState<Record<string, string>>({});
  const [isSaving, setSaving] = useState(false);
  const [editError, setEditError] = useState(false);
  const operationKey = `clinic.operationsAssistant.manage.operations.${operation}`;
  const operationLabel = t(operationKey) === operationKey ? operation : t(operationKey);
  const fieldLabel = (name: string) => {
    const key = `clinic.operationsAssistant.manage.fields.${name}`;
    return t(key) === key ? name : t(key);
  };
  const editable = fields.filter(([, value]) => typeof value !== "boolean");

  const startEdit = () => {
    setDraft(Object.fromEntries(editable.map(([name, value]) => [name, String(value)])));
    setEditError(false);
    setEditing(true);
  };

  const save = async () => {
    if (!onRevise) return;
    const next: Record<string, unknown> = { ...payload };
    for (const [name, original] of editable) {
      const text = draft[name] ?? String(original);
      if (typeof original === "number") {
        const parsed = Number(text);
        if (!Number.isFinite(parsed)) { setEditError(true); return; }
        next[name] = parsed;
      } else {
        next[name] = text;
      }
    }
    setSaving(true);
    setEditError(false);
    try {
      await onRevise({ ...asRecord(reviewPayload), operation, payload: next });
      setEditing(false);
    } catch {
      setEditError(true);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="flex flex-col gap-3">
      <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div><dt className="text-muted">{t("clinic.operationsAssistant.manage.operation")}</dt><dd className="font-medium">{operationLabel}</dd></div>
        {isEditing ? null : fields.map(([name, value]) => (
          <div key={name}><dt className="text-muted">{fieldLabel(name)}</dt><dd>{String(value)}</dd></div>
        ))}
      </dl>
      {isEditing ? (
        <form className="grid grid-cols-1 gap-3 sm:grid-cols-2" onSubmit={(event) => { event.preventDefault(); void save(); }}>
          {editable.map(([name, value]) => (
            <TextField key={name}>
              <Label>{fieldLabel(name)}</Label>
              <Input
                inputMode={typeof value === "number" ? "numeric" : undefined}
                value={draft[name] ?? ""}
                onChange={(event) => setDraft((current) => ({ ...current, [name]: event.target.value }))}
              />
            </TextField>
          ))}
          {editError ? <p role="alert" className="text-danger sm:col-span-2">{t("clinic.operationsAssistant.manage.editError")}</p> : null}
          <div className="flex flex-wrap gap-2 sm:col-span-2">
            <Tooltip><Button type="submit" variant="primary" isPending={isSaving} isDisabled={isSaving}><FaIcon icon="floppy-disk" aria-hidden="true" />{t("clinic.operationsAssistant.manage.save")}</Button><Tooltip.Content>{t("clinic.operationsAssistant.manage.saveTooltip")}</Tooltip.Content></Tooltip>
            <Tooltip><Button variant="tertiary" isDisabled={isSaving} onPress={() => setEditing(false)}><FaIcon icon="xmark" aria-hidden="true" />{t("clinic.operationsAssistant.manage.cancelEdit")}</Button><Tooltip.Content>{t("clinic.operationsAssistant.manage.cancelEditTooltip")}</Tooltip.Content></Tooltip>
          </div>
        </form>
      ) : onRevise && editable.length > 0 ? (
        <div>
          <Tooltip>
            <Button variant="outline" onPress={startEdit}><FaIcon icon="pen" aria-hidden="true" />{t("clinic.operationsAssistant.manage.edit")}</Button>
            <Tooltip.Content>{t("clinic.operationsAssistant.manage.editTooltip")}</Tooltip.Content>
          </Tooltip>
        </div>
      ) : null}
    </div>
  );
}

/** A staff invitation proposal: the full contact the owner confirms, and the fixed staff role. */
export function StaffInviteActionCard({ reviewPayload }: { reviewPayload: unknown }) {
  const { t } = useLocalizedText();
  const review = asRecord(reviewPayload);
  const rows: [string, unknown][] = [
    [t("clinic.operationsAssistant.invite.firstName"), review.firstName],
    [t("clinic.operationsAssistant.invite.lastName"), review.lastName],
    [t("clinic.operationsAssistant.invite.phone"), review.phoneNumber],
    [t("clinic.operationsAssistant.invite.email"), review.email],
    [t("clinic.operationsAssistant.invite.role"), t("clinic.operationsAssistant.invite.staff")],
  ];
  return (
    <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      {rows.filter(([, value]) => typeof value === "string" && value.length > 0).map(([label, value]) => (
        <div key={label}><dt className="text-muted">{label}</dt><dd dir="ltr" className="text-start">{String(value)}</dd></div>
      ))}
    </dl>
  );
}
