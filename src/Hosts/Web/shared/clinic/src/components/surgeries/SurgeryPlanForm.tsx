"use client";

import { useCallback, useMemo, useState, type FormEvent } from "react";
import {
  Alert,
  Button,
  Calendar,
  Chip,
  DateField,
  DatePicker,
  FieldError,
  Input,
  Label,
  ListBox,
  NumberField,
  Select,
  Surface,
  TextField,
  TimeField,
  Tooltip,
} from "@heroui/react";
import { Time } from "@internationalized/date";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { dateValueToIsoDate, parseIsoDateValue, useLocalizedText } from "../../localization/index";
import {
  applySurgeryType,
  scheduledInstant,
  validateSurgeryPlanForm,
  type SurgeryPlanErrors,
  type SurgeryPlanFormValues,
} from "../../models/surgeries/surgeryPlanForm";
import type { ClinicStaffMember } from "../../models/surgeries/surgeryModels";
import { useSurgeryTypes } from "../../hooks/surgeries/useSurgeryTypes";
import { useSurgerySuggestions } from "../../hooks/surgeries/useSurgerySuggestions";
import { useSurgeryConflicts } from "../../hooks/surgeries/useSurgeryConflicts";
import { SurgeryConflictAlert } from "./SurgeryConflictAlert";
import { SurgeryReminderEditor } from "./SurgeryReminderEditor";
import {
  SurgerySuggestionComboBox,
  SurgeryTeamEditor,
} from "./SurgeryTeamEditor";

export interface SurgeryPlanFormProps {
  mode: "create" | "edit";
  initialValues: SurgeryPlanFormValues;
  timeZone: string;
  staff: readonly ClinicStaffMember[];
  staffLoading?: boolean;
  excludeSurgeryId?: string;
  isSubmitting: boolean;
  submitError?: string | null;
  onSubmit: (values: SurgeryPlanFormValues) => void;
  onCancel: () => void;
}

function parseTimeFieldValue(value: string): Time | null {
  const match = /^(\d{1,2}):(\d{2})/.exec(value);
  if (!match) return null;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  if (hour > 23 || minute > 59) return null;
  return new Time(hour, minute);
}

function timeFieldValueToString(value: { hour: number; minute: number } | null): string {
  if (!value) return "";
  return `${String(value.hour).padStart(2, "0")}:${String(value.minute).padStart(2, "0")}`;
}

function SectionHeader({
  icon,
  title,
  description,
}: {
  icon: string;
  title: string;
  description?: string;
}) {
  return (
    <div className="flex items-center gap-3">
      <HeaderIcon icon={icon} />
      <div className="min-w-0">
        <h2 className="text-lg font-semibold text-foreground">{title}</h2>
        {description ? <p className="text-sm text-muted">{description}</p> : null}
      </div>
    </div>
  );
}

/** The create/edit surgery plan: surgery details, team, tools, and reminders. */
export function SurgeryPlanForm({
  mode,
  initialValues,
  timeZone,
  staff,
  staffLoading,
  excludeSurgeryId,
  isSubmitting,
  submitError,
  onSubmit,
  onCancel,
}: SurgeryPlanFormProps) {
  const { t } = useLocalizedText();
  const [values, setValues] = useState<SurgeryPlanFormValues>(initialValues);
  const [errors, setErrors] = useState<SurgeryPlanErrors>({});
  const [attempted, setAttempted] = useState(false);
  const [toolDraft, setToolDraft] = useState("");

  const typesQuery = useSurgeryTypes();
  const suggestionsQuery = useSurgerySuggestions();
  const surgeryTypes = typesQuery.types;
  const selectedType = surgeryTypes.find((type) => type.id === values.catalogId);

  const update = useCallback((patch: Partial<SurgeryPlanFormValues>) => {
    setValues((prev) => ({ ...prev, ...patch }));
    setErrors((prev) => {
      const next = { ...prev };
      for (const key of Object.keys(patch) as (keyof SurgeryPlanFormValues)[]) {
        delete next[key];
      }
      return next;
    });
  }, []);

  const roleSuggestions = useMemo(() => {
    const merged = new Set<string>();
    for (const role of suggestionsQuery.data?.roles ?? []) merged.add(role);
    for (const role of selectedType?.defaultTeamRoles ?? []) merged.add(role);
    return Array.from(merged);
  }, [suggestionsQuery.data, selectedType]);

  const locationSuggestions = suggestionsQuery.data?.locations ?? [];

  const scheduledAt = scheduledInstant(values, timeZone);
  const parsedDuration =
    values.durationMinutes.trim() === "" ? undefined : Number(values.durationMinutes);
  const durationMinutes =
    parsedDuration !== undefined && Number.isFinite(parsedDuration)
      ? parsedDuration
      : undefined;
  const teamUserIds = useMemo(
    () => values.team.map((row) => row.userId).filter((id) => id.length > 0),
    [values.team],
  );
  const conflictProbe = useMemo(
    () => ({
      scheduledAt,
      durationMinutes,
      location: values.location.trim() || undefined,
      teamUserIds,
      excludeSurgeryId,
    }),
    [scheduledAt, durationMinutes, values.location, teamUserIds, excludeSurgeryId],
  );
  const conflictsQuery = useSurgeryConflicts(conflictProbe);

  const handleTypeChange = (typeId: string) => {
    const type = surgeryTypes.find((candidate) => candidate.id === typeId);
    setValues((prev) => (type ? applySurgeryType(prev, type) : { ...prev, catalogId: typeId }));
    setErrors((prev) => ({
      ...prev,
      catalogId: undefined,
      name: undefined,
      team: undefined,
      tools: undefined,
      reminderPolicies: undefined,
    }));
  };

  const addTool = () => {
    const tool = toolDraft.trim();
    if (!tool) return;
    setValues((prev) =>
      prev.tools.some(
        (item) => item.localeCompare(tool, undefined, { sensitivity: "accent" }) === 0,
      )
        ? prev
        : { ...prev, tools: [...prev.tools, tool] },
    );
    setToolDraft("");
  };

  const removeTool = (tool: string) =>
    update({ tools: values.tools.filter((item) => item !== tool) });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setAttempted(true);
    const nextErrors = validateSurgeryPlanForm(values);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;
    onSubmit(values);
  };

  const hasErrors = attempted && Object.keys(errors).length > 0;
  const submitLabel = mode === "create"
    ? t("clinic.surgeries.plan.submitCreate")
    : t("clinic.surgeries.plan.submitEdit");

  return (
    <form className="flex flex-col gap-6" noValidate onSubmit={handleSubmit}>
      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <SectionHeader
          description={t("clinic.surgeries.plan.surgerySectionDescription")}
          icon="notes-medical"
          title={t("clinic.surgeries.plan.surgerySectionTitle")}
        />
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <Select
              aria-label={t("clinic.surgeries.fields.type")}
              isDisabled={typesQuery.isLoading}
              placeholder={t("clinic.surgeries.plan.typePlaceholder")}
              value={values.catalogId || null}
              variant="secondary"
              onChange={(key) => handleTypeChange(key == null ? "" : String(key))}
            >
              <Label>{t("clinic.surgeries.fields.type")}</Label>
              <Select.Trigger>
                <Select.Value className="min-w-0 truncate" />
                <Select.Indicator />
              </Select.Trigger>
              <Select.Popover>
                <ListBox>
                  {surgeryTypes.map((type) => (
                    <ListBox.Item key={type.id} id={type.id} textValue={type.name}>
                      {type.name}
                      <ListBox.ItemIndicator />
                    </ListBox.Item>
                  ))}
                </ListBox>
              </Select.Popover>
            </Select>
            {typesQuery.error ? (
              <p className="text-sm text-danger" role="alert">
                {t("clinic.surgeries.plan.typeUnavailable")}
              </p>
            ) : null}
            {errors.catalogId ? (
              <p className="text-sm text-danger" role="alert">
                {t(errors.catalogId)}
              </p>
            ) : null}
          </div>

          <TextField
            isInvalid={Boolean(errors.name)}
            value={values.name}
            variant="secondary"
            onChange={(name) => update({ name })}
          >
            <Label>{t("clinic.surgeries.fields.name")}</Label>
            <Input />
            {errors.name ? <FieldError>{t(errors.name)}</FieldError> : null}
          </TextField>

          <div className="flex flex-col gap-1.5">
            <DatePicker
              value={parseIsoDateValue(values.scheduledDate)}
              onChange={(value) => update({ scheduledDate: dateValueToIsoDate(value) })}
            >
              <Label>{t("clinic.surgeries.fields.date")}</Label>
              <DateField.Group fullWidth variant="secondary">
                <DateField.Input>
                  {(segment) => <DateField.Segment segment={segment} />}
                </DateField.Input>
                <DateField.Suffix>
                  <DatePicker.Trigger>
                    <DatePicker.TriggerIndicator />
                  </DatePicker.Trigger>
                </DateField.Suffix>
              </DateField.Group>
              <DatePicker.Popover>
                <Calendar aria-label={t("clinic.surgeries.fields.date")}>
                  <Calendar.Header>
                    <Calendar.NavButton slot="previous" />
                    <Calendar.Heading />
                    <Calendar.NavButton slot="next" />
                  </Calendar.Header>
                  <Calendar.Grid>
                    <Calendar.GridHeader>
                      {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
                    </Calendar.GridHeader>
                    <Calendar.GridBody>
                      {(date) => <Calendar.Cell date={date} />}
                    </Calendar.GridBody>
                  </Calendar.Grid>
                </Calendar>
              </DatePicker.Popover>
            </DatePicker>
            {errors.scheduledDate ? (
              <p className="text-sm text-danger" role="alert">
                {t(errors.scheduledDate)}
              </p>
            ) : null}
          </div>

          <div className="flex flex-col gap-1.5">
            <TimeField
              value={parseTimeFieldValue(values.scheduledTime)}
              onChange={(value) =>
                update({ scheduledTime: timeFieldValueToString(value) })
              }
            >
              <Label>{t("clinic.surgeries.fields.time")}</Label>
              <TimeField.Group variant="secondary">
                <TimeField.Input>
                  {(segment) => <TimeField.Segment segment={segment} />}
                </TimeField.Input>
              </TimeField.Group>
            </TimeField>
            {errors.scheduledTime ? (
              <p className="text-sm text-danger" role="alert">
                {t(errors.scheduledTime)}
              </p>
            ) : null}
          </div>

          <div className="flex flex-col gap-1.5">
            <NumberField
              aria-label={t("clinic.surgeries.fields.durationMinutes")}
              minValue={0}
              value={durationMinutes ?? NaN}
              variant="secondary"
              onChange={(value) =>
                update({
                  durationMinutes: Number.isFinite(value) ? String(value) : "",
                })
              }
            >
              <Label>{t("clinic.surgeries.fields.durationMinutes")}</Label>
              <NumberField.Input />
            </NumberField>
            {errors.durationMinutes ? (
              <p className="text-sm text-danger" role="alert">
                {t(errors.durationMinutes)}
              </p>
            ) : null}
          </div>

          <div className="flex flex-col gap-1.5">
            <SurgerySuggestionComboBox
              label={t("clinic.surgeries.fields.location")}
              placeholder={t("clinic.surgeries.fields.location")}
              suggestions={locationSuggestions}
              value={values.location}
              onChange={(location) => update({ location })}
            />
            {errors.location ? (
              <p className="text-sm text-danger" role="alert">
                {t(errors.location)}
              </p>
            ) : null}
          </div>
        </div>
      </Surface>

      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <SectionHeader
          description={t("clinic.surgeries.plan.teamSectionDescription")}
          icon="users"
          title={t("clinic.surgeries.plan.teamSectionTitle")}
        />
        <SurgeryTeamEditor
          error={errors.team ? t(errors.team) : undefined}
          roleSuggestions={roleSuggestions}
          rows={values.team}
          staff={staff}
          staffLoading={staffLoading}
          onChange={(team) => update({ team })}
        />
      </Surface>

      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <SectionHeader
          description={t("clinic.surgeries.plan.toolsSectionDescription")}
          icon="screwdriver-wrench"
          title={t("clinic.surgeries.plan.toolsSectionTitle")}
        />
        <div className="flex flex-col gap-3">
          <div className="flex items-end gap-2">
            <TextField
              className="min-w-0 flex-1"
              value={toolDraft}
              variant="secondary"
              onChange={setToolDraft}
            >
              <Label>{t("clinic.surgeries.fields.tools")}</Label>
              <Input
                placeholder={t("clinic.surgeries.plan.toolPlaceholder")}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    addTool();
                  }
                }}
              />
            </TextField>
            <Tooltip>
              <Button
                aria-label={t("clinic.surgeries.plan.addTool")}
                isIconOnly
                type="button"
                variant="tertiary"
                onPress={addTool}
              >
                <FaIcon aria-hidden="true" icon="plus" />
              </Button>
              <Tooltip.Content>{t("clinic.surgeries.plan.addTool")}</Tooltip.Content>
            </Tooltip>
          </div>
          {values.tools.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {values.tools.map((tool) => (
                <Chip key={tool} size="sm" variant="soft">
                  <Chip.Label>{tool}</Chip.Label>
                  <Tooltip>
                    <Button
                      aria-label={t("clinic.surgeries.plan.removeTool", { tool })}
                      isIconOnly
                      size="sm"
                      type="button"
                      variant="ghost"
                      onPress={() => removeTool(tool)}
                    >
                      <FaIcon aria-hidden="true" icon="xmark" />
                    </Button>
                    <Tooltip.Content>
                      {t("clinic.surgeries.plan.removeTool", { tool })}
                    </Tooltip.Content>
                  </Tooltip>
                </Chip>
              ))}
            </div>
          ) : null}
          {errors.tools ? (
            <p className="text-sm text-danger" role="alert">
              {t(errors.tools)}
            </p>
          ) : null}
        </div>
      </Surface>

      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <SectionHeader
          description={t("clinic.surgeries.plan.remindersSectionDescription")}
          icon="bell"
          title={t("clinic.surgeries.plan.remindersSectionTitle")}
        />
        <SurgeryReminderEditor
          error={errors.reminderPolicies ? t(errors.reminderPolicies) : undefined}
          reminders={values.reminderPolicies}
          onChange={(reminderPolicies) => update({ reminderPolicies })}
        />
      </Surface>

      <SurgeryConflictAlert conflicts={conflictsQuery.data ?? []} />

      {submitError ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{submitError}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : null}

      {hasErrors ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("clinic.surgeries.plan.validationSummary")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : null}

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Tooltip>
          <Button
            isDisabled={isSubmitting}
            type="button"
            variant="ghost"
            onPress={onCancel}
          >
            <FaIcon aria-hidden="true" icon="xmark" />
            {t("common.actions.cancel")}
          </Button>
          <Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button
            isPending={isSubmitting}
            type="submit"
            variant="primary"
          >
            <FaIcon aria-hidden="true" icon="scissors" />
            {submitLabel}
          </Button>
          <Tooltip.Content>{submitLabel}</Tooltip.Content>
        </Tooltip>
      </div>
    </form>
  );
}
