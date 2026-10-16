"use client";

import { useMemo } from "react";
import {
  Button,
  ComboBox,
  Input,
  Label,
  ListBox,
  Select,
  Tooltip,
} from "@heroui/react";
import { FaIcon, removeButtonClassName } from "@vitarish/ui";
import { useLocalizedText } from "../../localization/index";
import type { ClinicStaffMember } from "../../models/surgeries/surgeryModels";
import type { SurgeryPlanTeamRow } from "../../models/surgeries/surgeryPlanForm";

export interface SurgerySuggestionComboBoxProps {
  label: string;
  value: string;
  suggestions: readonly string[];
  placeholder: string;
  onChange: (value: string) => void;
}

/**
 * Free-text field with suggestions, shared by the surgery's location and the
 * team role rows. Suggestions are hints only; the typed value is always kept.
 */
export function SurgerySuggestionComboBox({
  label,
  value,
  suggestions,
  placeholder,
  onChange,
}: SurgerySuggestionComboBoxProps) {
  const items = useMemo(
    () => suggestions.map((suggestion) => ({ id: suggestion, name: suggestion })),
    [suggestions],
  );

  return (
    <ComboBox
      allowsCustomValue
      defaultItems={items}
      inputValue={value}
      variant="secondary"
      onInputChange={onChange}
    >
      <Label>{label}</Label>
      <ComboBox.InputGroup>
        <Input placeholder={placeholder} />
        <ComboBox.Trigger />
      </ComboBox.InputGroup>
      <ComboBox.Popover>
        <ListBox>
          {(item: { id: string; name: string }) => (
            <ListBox.Item id={item.id} textValue={item.name}>
              {item.name}
              <ListBox.ItemIndicator />
            </ListBox.Item>
          )}
        </ListBox>
      </ComboBox.Popover>
    </ComboBox>
  );
}

export interface SurgeryTeamEditorProps {
  rows: SurgeryPlanTeamRow[];
  staff: readonly ClinicStaffMember[];
  staffLoading?: boolean;
  roleSuggestions: readonly string[];
  error?: string;
  onChange: (rows: SurgeryPlanTeamRow[]) => void;
}

/** Rows of clinic staff plus a free-text role, for the surgery's team. */
export function SurgeryTeamEditor({
  rows,
  staff,
  staffLoading,
  roleSuggestions,
  error,
  onChange,
}: SurgeryTeamEditorProps) {
  const { t } = useLocalizedText();

  const updateRow = (key: string, patch: Partial<SurgeryPlanTeamRow>) => {
    onChange(rows.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  };

  const removeRow = (key: string) => {
    onChange(rows.filter((row) => row.key !== key));
  };

  const addRow = () => {
    onChange([...rows, { key: crypto.randomUUID(), userId: "", role: "" }]);
  };

  return (
    <div className="flex flex-col gap-4">
      {rows.map((row) => (
        <div
          key={row.key}
          className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto] sm:items-end"
        >
          <div className="flex min-w-0 flex-col gap-1.5">
            <Select
              aria-label={t("clinic.surgeries.fields.member")}
              isDisabled={staffLoading}
              placeholder={t("clinic.surgeries.plan.memberPlaceholder")}
              value={row.userId || null}
              variant="secondary"
              onChange={(key) =>
                updateRow(row.key, { userId: key == null ? "" : String(key) })
              }
            >
              <Label>{t("clinic.surgeries.fields.member")}</Label>
              <Select.Trigger>
                <Select.Value className="min-w-0 truncate" />
                <Select.Indicator />
              </Select.Trigger>
              <Select.Popover>
                <ListBox>
                  {staff.map((member) => (
                    <ListBox.Item
                      key={member.userId}
                      id={member.userId}
                      textValue={member.displayName}
                    >
                      {member.displayName}
                      <ListBox.ItemIndicator />
                    </ListBox.Item>
                  ))}
                </ListBox>
              </Select.Popover>
            </Select>
          </div>
          <SurgerySuggestionComboBox
            label={t("clinic.surgeries.fields.role")}
            placeholder={t("clinic.surgeries.plan.rolePlaceholder")}
            suggestions={roleSuggestions}
            value={row.role}
            onChange={(role) => updateRow(row.key, { role })}
          />
          <Tooltip>
            <Button
              aria-label={t("clinic.surgeries.plan.removeMember")}
              className={`${removeButtonClassName} text-danger`}
              isIconOnly
              onPress={() => removeRow(row.key)}
              variant="ghost"
            >
              <FaIcon aria-hidden="true" icon="user-xmark" />
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.plan.removeMember")}
            </Tooltip.Content>
          </Tooltip>
        </div>
      ))}

      <div>
        <Tooltip>
          <Button onPress={addRow} variant="outline">
            <FaIcon aria-hidden="true" icon="user-plus" />
            {t("clinic.surgeries.plan.addMember")}
          </Button>
          <Tooltip.Content>{t("clinic.surgeries.plan.addMember")}</Tooltip.Content>
        </Tooltip>
      </div>

      {error ? (
        <p className="text-sm text-danger" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}
