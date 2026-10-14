"use client";

import { useState } from "react";
import {
  Alert,
  Button,
  Chip,
  ComboBox,
  FieldError,
  Input,
  Label,
  ListBox,
  Surface,
  TextField,
  Tooltip,
} from "@heroui/react";
import { FaIcon, HeaderIcon, removeButtonClassName } from "@vitarish/ui";
import { formatNumber, useLocalizedText } from "../../localization";
import type {
  SurgeryReminderPolicy,
  SurgeryType,
} from "../../models/surgeries/surgeryModels";

export interface SurgeryTypeEditorValues {
  name: string;
  tools: string[];
  defaultTeamRoles: string[];
  reminderPolicies: SurgeryReminderPolicy[];
}

export interface SurgeryTypeEditorProps {
  type: SurgeryType | null;
  roleSuggestions: readonly string[];
  isSaving: boolean;
  isRemoving: boolean;
  showBack: boolean;
  saveErrorMessage: string | null;
  deleteBlockedMessage: string | null;
  onBack: () => void;
  onSave: (values: SurgeryTypeEditorValues) => void;
  onRequestDelete: () => void;
}

interface ReminderRow {
  key: string;
  name: string;
  minutesBefore: string;
  subject: string;
  textBody: string;
}

interface ReminderRowErrors {
  minutes?: string;
  subject?: string;
  textBody?: string;
}

function toReminderRow(policy: SurgeryReminderPolicy): ReminderRow {
  return {
    key: crypto.randomUUID(),
    name: policy.name,
    minutesBefore: String(policy.minutesBefore),
    subject: policy.subject,
    textBody: policy.textBody,
  };
}

export function SurgeryTypeEditor({
  type,
  roleSuggestions,
  isSaving,
  isRemoving,
  showBack,
  saveErrorMessage,
  deleteBlockedMessage,
  onBack,
  onSave,
  onRequestDelete,
}: SurgeryTypeEditorProps) {
  const { t, locale } = useLocalizedText();
  const [name, setName] = useState(type?.name ?? "");
  const [nameError, setNameError] = useState<string | null>(null);
  const [tools, setTools] = useState<string[]>(type?.tools ?? []);
  const [toolDraft, setToolDraft] = useState("");
  const [roles, setRoles] = useState<string[]>(type?.defaultTeamRoles ?? []);
  const [roleDraft, setRoleDraft] = useState("");
  const [reminders, setReminders] = useState<ReminderRow[]>(() =>
    (type?.reminderPolicies ?? []).map(toReminderRow),
  );
  const [reminderErrors, setReminderErrors] = useState<
    Record<string, ReminderRowErrors>
  >({});

  const reset = () => {
    setName(type?.name ?? "");
    setNameError(null);
    setTools(type?.tools ?? []);
    setToolDraft("");
    setRoles(type?.defaultTeamRoles ?? []);
    setRoleDraft("");
    setReminders((type?.reminderPolicies ?? []).map(toReminderRow));
    setReminderErrors({});
  };

  const addTool = (value: string) => {
    const trimmed = value.trim();
    if (trimmed && !tools.includes(trimmed))
      setTools((current) => [...current, trimmed]);
    setToolDraft("");
  };

  const addRole = (value: string) => {
    const trimmed = value.trim();
    if (trimmed && !roles.includes(trimmed))
      setRoles((current) => [...current, trimmed]);
    setRoleDraft("");
  };

  const updateReminder = (key: string, patch: Partial<ReminderRow>) => {
    setReminders((current) =>
      current.map((row) => (row.key === key ? { ...row, ...patch } : row)),
    );
  };

  const reminderHint = (minutesText: string): string => {
    const minutes = Number(minutesText);
    if (!Number.isInteger(minutes) || minutes < 0) return "";
    if (minutes >= 1440 && minutes % 1440 === 0)
      return t("clinic.surgeries.types.reminderHint.days", {
        count: formatNumber(minutes / 1440, locale),
      });
    if (minutes >= 60 && minutes % 60 === 0)
      return t("clinic.surgeries.types.reminderHint.hours", {
        count: formatNumber(minutes / 60, locale),
      });
    return t("clinic.surgeries.types.reminderHint.minutes", {
      count: formatNumber(minutes, locale),
    });
  };

  const collectReminderErrors = (): Record<string, ReminderRowErrors> => {
    const errors: Record<string, ReminderRowErrors> = {};
    for (const row of reminders) {
      const rowErrors: ReminderRowErrors = {};
      const minutes = Number(row.minutesBefore);
      if (
        row.minutesBefore.trim() === "" ||
        !Number.isInteger(minutes) ||
        minutes < 0
      )
        rowErrors.minutes = t(
          "clinic.surgeries.types.reminderMinutesInvalid",
        );
      if (!row.subject.trim())
        rowErrors.subject = t(
          "clinic.surgeries.types.reminderSubjectRequired",
        );
      if (!row.textBody.trim())
        rowErrors.textBody = t(
          "clinic.surgeries.types.reminderMessageRequired",
        );
      if (Object.keys(rowErrors).length > 0) errors[row.key] = rowErrors;
    }
    return errors;
  };

  const handleSave = () => {
    const trimmedName = name.trim();
    const trimmedNameError = trimmedName
      ? null
      : t("clinic.surgeries.types.nameRequired");
    const errors = collectReminderErrors();
    setNameError(trimmedNameError);
    setReminderErrors(errors);
    if (trimmedNameError || Object.keys(errors).length > 0) return;
    onSave({
      name: trimmedName,
      tools,
      defaultTeamRoles: roles,
      reminderPolicies: reminders.map((row) => {
        const subject = row.subject.trim();
        return {
          name: row.name.trim() || subject,
          minutesBefore: Number(row.minutesBefore),
          subject,
          textBody: row.textBody.trim(),
        };
      }),
    });
  };

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="scissors" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">
              {t(
                type
                  ? "clinic.surgeries.types.editTitle"
                  : "clinic.surgeries.types.createTitle",
              )}
            </h2>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {showBack && (
            <Tooltip>
              <Button
                variant="tertiary"
                className="lg:hidden"
                onPress={onBack}
                aria-label={t("clinic.surgeries.types.backToListTooltip")}
              >
                <FaIcon
                  icon="arrow-left"
                  aria-hidden="true"
                  className="rtl:rotate-180"
                />
                {t("clinic.surgeries.types.backToList")}
              </Button>
              <Tooltip.Content>
                {t("clinic.surgeries.types.backToListTooltip")}
              </Tooltip.Content>
            </Tooltip>
          )}
          {!deleteBlockedMessage && (
            <Tooltip>
              <Button
                variant="ghost"
                className={`${removeButtonClassName} text-danger`}
                isDisabled={isRemoving}
                onPress={onRequestDelete}
              >
                <FaIcon icon="trash" aria-hidden="true" />
                {t("clinic.surgeries.types.delete")}
              </Button>
              <Tooltip.Content>
                {t("clinic.surgeries.types.deleteTooltip")}
              </Tooltip.Content>
            </Tooltip>
          )}
          <Tooltip>
            <Button variant="tertiary" onPress={reset}>
              <FaIcon icon="rotate-left" aria-hidden="true" />
              {t("clinic.surgeries.types.discard")}
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.types.discardTooltip")}
            </Tooltip.Content>
          </Tooltip>
          <Tooltip>
            <Button variant="primary" isPending={isSaving} onPress={handleSave}>
              <FaIcon icon="floppy-disk" aria-hidden="true" />
              {t("clinic.surgeries.types.save")}
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.types.saveTooltip")}
            </Tooltip.Content>
          </Tooltip>
        </div>
      </div>

      {saveErrorMessage && (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{saveErrorMessage}</Alert.Title>
          </Alert.Content>
        </Alert>
      )}
      {deleteBlockedMessage && (
        <Alert status="warning">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{deleteBlockedMessage}</Alert.Title>
          </Alert.Content>
        </Alert>
      )}

      <TextField
        value={name}
        onChange={(value) => {
          setName(value);
          if (nameError) setNameError(null);
        }}
        isInvalid={!!nameError}
        isRequired
        variant="secondary"
      >
        <Label>{t("clinic.surgeries.types.nameLabel")}</Label>
        <Input placeholder={t("clinic.surgeries.types.namePlaceholder")} />
        {nameError && <FieldError>{nameError}</FieldError>}
      </TextField>

      <div className="flex flex-col gap-3">
        <p className="text-sm font-medium text-foreground">
          {t("clinic.surgeries.types.toolsLabel")}
        </p>
        {tools.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {tools.map((tool) => (
              <Chip key={tool} size="lg" variant="soft" className="gap-1">
                {tool}
                <Tooltip>
                  <Button
                    isIconOnly
                    size="sm"
                    variant="ghost"
                    aria-label={t("clinic.surgeries.types.removeTool", {
                      name: tool,
                    })}
                    onPress={() =>
                      setTools((current) =>
                        current.filter((value) => value !== tool),
                      )
                    }
                  >
                    <FaIcon icon="xmark" aria-hidden="true" />
                  </Button>
                  <Tooltip.Content>
                    {t("clinic.surgeries.types.removeTool", { name: tool })}
                  </Tooltip.Content>
                </Tooltip>
              </Chip>
            ))}
          </div>
        )}
        <div className="flex items-end gap-2">
          <TextField
            value={toolDraft}
            onChange={setToolDraft}
            className="flex-1"
            variant="secondary"
            aria-label={t("clinic.surgeries.types.toolsLabel")}
          >
            <Input placeholder={t("clinic.surgeries.types.toolsPlaceholder")} />
          </TextField>
          <Tooltip>
            <Button
              isIconOnly
              variant="tertiary"
              aria-label={t("clinic.surgeries.types.addTool")}
              onPress={() => addTool(toolDraft)}
            >
              <FaIcon icon="plus" aria-hidden="true" />
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.types.addTool")}
            </Tooltip.Content>
          </Tooltip>
        </div>
      </div>

      <div className="flex flex-col gap-3">
        <p className="text-sm font-medium text-foreground">
          {t("clinic.surgeries.types.rolesLabel")}
        </p>
        {roles.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {roles.map((role) => (
              <Chip key={role} size="lg" variant="soft" className="gap-1">
                {role}
                <Tooltip>
                  <Button
                    isIconOnly
                    size="sm"
                    variant="ghost"
                    aria-label={t("clinic.surgeries.types.removeRole", {
                      name: role,
                    })}
                    onPress={() =>
                      setRoles((current) =>
                        current.filter((value) => value !== role),
                      )
                    }
                  >
                    <FaIcon icon="xmark" aria-hidden="true" />
                  </Button>
                  <Tooltip.Content>
                    {t("clinic.surgeries.types.removeRole", { name: role })}
                  </Tooltip.Content>
                </Tooltip>
              </Chip>
            ))}
          </div>
        )}
        <div className="flex items-end gap-2">
          <ComboBox
            className="flex-1"
            aria-label={t("clinic.surgeries.types.rolesLabel")}
            allowsCustomValue
            inputValue={roleDraft}
            onInputChange={setRoleDraft}
            selectedKey={null}
            onSelectionChange={(key) => {
              if (key != null) addRole(String(key));
            }}
          >
            <ComboBox.InputGroup>
              <Input
                placeholder={t("clinic.surgeries.types.rolesPlaceholder")}
              />
              <ComboBox.Trigger />
            </ComboBox.InputGroup>
            <ComboBox.Popover>
              <ListBox>
                {roleSuggestions.map((role) => (
                  <ListBox.Item key={role} id={role} textValue={role}>
                    {role}
                  </ListBox.Item>
                ))}
              </ListBox>
            </ComboBox.Popover>
          </ComboBox>
          <Tooltip>
            <Button
              isIconOnly
              variant="tertiary"
              aria-label={t("clinic.surgeries.types.addRole")}
              onPress={() => addRole(roleDraft)}
            >
              <FaIcon icon="plus" aria-hidden="true" />
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.types.addRole")}
            </Tooltip.Content>
          </Tooltip>
        </div>
      </div>

      <div className="flex flex-col gap-4">
        <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div className="min-w-0">
            <p className="text-sm font-medium text-foreground">
              {t("clinic.surgeries.types.remindersLabel")}
            </p>
            <p className="text-sm text-muted">
              {t("clinic.surgeries.types.remindersDescription")}
            </p>
          </div>
          <Tooltip>
            <Button
              variant="tertiary"
              onPress={() =>
                setReminders((current) => [
                  ...current,
                  {
                    key: crypto.randomUUID(),
                    name: "",
                    minutesBefore: "60",
                    subject: "",
                    textBody: "",
                  },
                ])
              }
            >
              <FaIcon icon="plus" aria-hidden="true" />
              {t("clinic.surgeries.types.addReminder")}
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.types.addReminder")}
            </Tooltip.Content>
          </Tooltip>
        </div>
        <p className="text-sm text-muted">
          {t("clinic.surgeries.types.remindersNote")}
        </p>
        {reminders.map((row) => {
          const rowErrors = reminderErrors[row.key] ?? {};
          const hint = reminderHint(row.minutesBefore);
          return (
            <Surface
              key={row.key}
              variant="secondary"
              className="flex flex-col gap-4 rounded-2xl p-4"
            >
              <div className="flex items-start justify-between gap-3">
                <p className="text-sm text-muted">
                  {hint || t("clinic.surgeries.types.reminderMinutesLabel")}
                </p>
                <Tooltip>
                  <Button
                    isIconOnly
                    size="sm"
                    variant="ghost"
                    className={`${removeButtonClassName} text-danger`}
                    aria-label={t("clinic.surgeries.types.removeReminder")}
                    onPress={() =>
                      setReminders((current) =>
                        current.filter((value) => value.key !== row.key),
                      )
                    }
                  >
                    <FaIcon icon="trash" aria-hidden="true" />
                  </Button>
                  <Tooltip.Content>
                    {t("clinic.surgeries.types.removeReminder")}
                  </Tooltip.Content>
                </Tooltip>
              </div>
              <div className="grid gap-4 sm:grid-cols-3">
                <TextField
                  value={row.minutesBefore}
                  onChange={(value) =>
                    updateReminder(row.key, { minutesBefore: value })
                  }
                  isInvalid={!!rowErrors.minutes}
                  variant="secondary"
                >
                  <Label>
                    {t("clinic.surgeries.types.reminderMinutesLabel")}
                  </Label>
                  <Input inputMode="numeric" />
                  {rowErrors.minutes && (
                    <FieldError>{rowErrors.minutes}</FieldError>
                  )}
                </TextField>
                <TextField
                  value={row.subject}
                  onChange={(value) =>
                    updateReminder(row.key, { subject: value })
                  }
                  isInvalid={!!rowErrors.subject}
                  variant="secondary"
                >
                  <Label>
                    {t("clinic.surgeries.types.reminderSubjectLabel")}
                  </Label>
                  <Input />
                  {rowErrors.subject && (
                    <FieldError>{rowErrors.subject}</FieldError>
                  )}
                </TextField>
                <TextField
                  value={row.textBody}
                  onChange={(value) =>
                    updateReminder(row.key, { textBody: value })
                  }
                  isInvalid={!!rowErrors.textBody}
                  variant="secondary"
                >
                  <Label>
                    {t("clinic.surgeries.types.reminderMessageLabel")}
                  </Label>
                  <Input />
                  {rowErrors.textBody && (
                    <FieldError>{rowErrors.textBody}</FieldError>
                  )}
                </TextField>
              </div>
            </Surface>
          );
        })}
      </div>
    </Surface>
  );
}
