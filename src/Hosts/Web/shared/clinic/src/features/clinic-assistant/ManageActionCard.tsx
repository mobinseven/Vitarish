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

/** A staff invitation proposal: the full contact the owner confirms, the fixed staff role, and an edit form. */
export function StaffInviteActionCard({
  reviewPayload,
  onRevise,
}: {
  reviewPayload: unknown;
  /** Present while the invitation can still be edited; resolves when the edit was saved as a new proposal. */
  onRevise?: (argumentsJson: Record<string, unknown>) => Promise<void>;
}) {
  const { t } = useLocalizedText();
  const review = asRecord(reviewPayload);
  const contactField = typeof review.email === "string" ? "email" : "phoneNumber";
  const editable: [string, string][] = [
    ["firstName", t("clinic.operationsAssistant.invite.firstName")],
    ["lastName", t("clinic.operationsAssistant.invite.lastName")],
    [contactField, contactField === "email" ? t("clinic.operationsAssistant.invite.email") : t("clinic.operationsAssistant.invite.phone")],
  ];
  const [draft, setDraft] = useState<Record<string, string> | null>(null);
  const [isSaving, setSaving] = useState(false);
  const [editError, setEditError] = useState(false);
  const rows: [string, unknown][] = [
    [t("clinic.operationsAssistant.invite.firstName"), review.firstName],
    [t("clinic.operationsAssistant.invite.lastName"), review.lastName],
    [t("clinic.operationsAssistant.invite.phone"), review.phoneNumber],
    [t("clinic.operationsAssistant.invite.email"), review.email],
    [t("clinic.operationsAssistant.invite.role"), t("clinic.operationsAssistant.invite.staff")],
  ];

  const save = async () => {
    if (!onRevise || !draft) return;
    setSaving(true);
    setEditError(false);
    try {
      // Exactly the tool's arguments: one contact and both names; the role is never an input.
      await onRevise({ firstName: draft.firstName ?? "", lastName: draft.lastName ?? "", [contactField]: draft[contactField] ?? "" });
      setDraft(null);
    } catch {
      setEditError(true);
    } finally {
      setSaving(false);
    }
  };

  if (draft) {
    return (
      <form className="grid grid-cols-1 gap-3 sm:grid-cols-2" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        {editable.map(([name, label]) => (
          <TextField key={name}>
            <Label>{label}</Label>
            <Input dir={name === contactField ? "ltr" : undefined} value={draft[name] ?? ""} onChange={(event) => setDraft((current) => ({ ...current, [name]: event.target.value }))} />
          </TextField>
        ))}
        {editError ? <p role="alert" className="text-danger sm:col-span-2">{t("clinic.operationsAssistant.manage.editError")}</p> : null}
        <div className="flex flex-wrap gap-2 sm:col-span-2">
          <Tooltip><Button type="submit" variant="primary" isPending={isSaving} isDisabled={isSaving}><FaIcon icon="floppy-disk" aria-hidden="true" />{t("clinic.operationsAssistant.manage.save")}</Button><Tooltip.Content>{t("clinic.operationsAssistant.manage.saveTooltip")}</Tooltip.Content></Tooltip>
          <Tooltip><Button variant="tertiary" isDisabled={isSaving} onPress={() => setDraft(null)}><FaIcon icon="xmark" aria-hidden="true" />{t("clinic.operationsAssistant.manage.cancelEdit")}</Button><Tooltip.Content>{t("clinic.operationsAssistant.manage.cancelEditTooltip")}</Tooltip.Content></Tooltip>
        </div>
      </form>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        {rows.filter(([, value]) => typeof value === "string" && value.length > 0).map(([label, value]) => (
          <div key={label}><dt className="text-muted">{label}</dt><dd dir="ltr" className="text-start">{String(value)}</dd></div>
        ))}
      </dl>
      {onRevise ? (
        <div>
          <Tooltip>
            <Button variant="outline" onPress={() => { setEditError(false); setDraft(Object.fromEntries(editable.map(([name]) => [name, typeof review[name] === "string" ? String(review[name]) : ""]))); }}>
              <FaIcon icon="pen" aria-hidden="true" />{t("clinic.operationsAssistant.manage.edit")}
            </Button>
            <Tooltip.Content>{t("clinic.operationsAssistant.manage.editTooltip")}</Tooltip.Content>
          </Tooltip>
        </div>
      ) : null}
    </div>
  );
}

/** Label, value and whether the value reads left to right (contacts, phones, addresses on the web). */
type ReviewRow = [string, unknown, boolean?];

function ReviewRows({ rows }: { rows: ReviewRow[] }) {
  return (
    <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      {rows.filter(([, value]) => typeof value === "string" && value.length > 0).map(([label, value, ltr]) => (
        <div key={label}><dt className="text-muted">{label}</dt><dd dir={ltr ? "ltr" : undefined} className="text-start">{String(value)}</dd></div>
      ))}
    </dl>
  );
}

/** A prepared staff removal: the member as the server found them. */
export function StaffRemovalActionCard({ reviewPayload }: { reviewPayload: unknown }) {
  const { t } = useLocalizedText();
  const review = asRecord(reviewPayload);
  return (
    <ReviewRows rows={[
      [t("clinic.operationsAssistant.removeMember.member"), review.name],
      [t("clinic.operationsAssistant.removeMember.contact"), review.contact, true],
    ]} />
  );
}

/** A prepared clinic-profile change: only the parts that change. */
export function ClinicProfileActionCard({ reviewPayload }: { reviewPayload: unknown }) {
  const { t } = useLocalizedText();
  const review = asRecord(reviewPayload);
  const address = asRecord(review.address);
  const separator = t("clinic.operationsAssistant.clinicProfile.separator");
  const texts = (values: unknown[]) => values.filter((value): value is string => typeof value === "string" && value.length > 0);
  const addressText = texts([address.province, address.city, address.street, address.postalCode]).join(separator);
  const phones = Array.isArray(review.phones) ? texts(review.phones).join(separator) : undefined;
  return (
    <ReviewRows rows={[
      [t("clinic.operationsAssistant.clinicProfile.description"), review.description],
      [t("clinic.operationsAssistant.clinicProfile.address"), addressText],
      [t("clinic.operationsAssistant.clinicProfile.phones"), phones, true],
      [t("clinic.operationsAssistant.clinicProfile.website"), review.website, true],
    ]} />
  );
}
