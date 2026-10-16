"use client";

import { useCallback, useState } from "react";
import {
  Button,
  Calendar,
  Chip,
  DateField,
  DatePicker,
  FieldError,
  Input,
  Label,
  TextField,
  Tooltip,
} from "@heroui/react";
import { NativeSelect } from "@heroui-pro/react";
import {
  dateValueToIsoDate,
  parseIsoDateValue,
} from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import type {
  PatientProfileFormData,
  PatientProfileFormErrors,
} from "../../../models/patients/profile/PatientProfileForm";
import { useLocalizedText } from "../../../localization/index";

interface PatientProfileFormProps {
  initialData: PatientProfileFormData;
  onSubmit: (data: PatientProfileFormData) => void;
  onCancel: () => void;
  isPending?: boolean;
}

export function PatientProfileForm({
  initialData,
  onSubmit,
  onCancel,
  isPending = false,
}: PatientProfileFormProps) {
  const { t } = useLocalizedText();
  const [formData, setFormData] = useState<PatientProfileFormData>(initialData);
  const [errors, setErrors] = useState<PatientProfileFormErrors>({});
  const [tagDraft, setTagDraft] = useState("");

  const handleChange = useCallback(
    (field: keyof PatientProfileFormData, value: string) => {
      setFormData((prev) => ({ ...prev, [field]: value }));
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    },
    [],
  );

  const handleSubmit = useCallback(() => {
    const newErrors: PatientProfileFormErrors = {};
    if (!formData.firstName.trim())
      newErrors.firstName = t(
        "clinic.patient.edit.validation.firstNameRequired",
      );
    if (!formData.lastName.trim())
      newErrors.lastName = t("clinic.patient.edit.validation.lastNameRequired");
    if (!formData.identificationCode.trim())
      newErrors.identificationCode = t(
        "clinic.patient.edit.validation.identificationCodeRequired",
      );
    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = t("clinic.patient.edit.validation.invalidEmail");
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    onSubmit(formData);
  }, [formData, onSubmit, t]);

  const addTag = useCallback(() => {
    const tag = tagDraft.trim();
    if (!tag) return;
    setFormData((prev) => {
      if (prev.tags.some((item) => item.localeCompare(tag, undefined, { sensitivity: "accent" }) === 0)) {
        return prev;
      }
      return { ...prev, tags: [...prev.tags, tag] };
    });
    setTagDraft("");
  }, [tagDraft]);

  const removeTag = useCallback((tag: string) => {
    setFormData((prev) => ({ ...prev, tags: prev.tags.filter((item) => item !== tag) }));
  }, []);

  return (
    <form
      noValidate
      className="flex flex-col gap-8"
      onSubmit={(e) => {
        e.preventDefault();
        handleSubmit();
      }}
    >
      <section>
        <h2 className="mb-4 text-sm font-semibold text-foreground">
          {t("clinic.patient.sections.personal")}
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormTextField
            label={t("clinic.patient.fields.firstName")}
            value={formData.firstName}
            onChange={(v) => handleChange("firstName", v)}
            error={errors.firstName}
            isRequired
          />
          <FormTextField
            label={t("clinic.patient.fields.lastName")}
            value={formData.lastName}
            onChange={(v) => handleChange("lastName", v)}
            error={errors.lastName}
            isRequired
          />
          <FormTextField
            label={t("clinic.patient.fields.identificationCode")}
            value={formData.identificationCode}
            onChange={(v) => handleChange("identificationCode", v)}
            error={errors.identificationCode}
            isRequired
          />

          <DatePicker
            className="w-72"
            value={parseIsoDateValue(formData.dateOfBirth)}
            onChange={(value) =>
              handleChange("dateOfBirth", dateValueToIsoDate(value))
            }
          >
            <Label>{t("clinic.patient.fields.dateOfBirth")}</Label>
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
              <Calendar aria-label={t("clinic.patient.fields.dateOfBirth")}>
                <Calendar.Header>
                  <Calendar.YearPickerTrigger>
                    <Calendar.YearPickerTriggerHeading />
                    <Calendar.YearPickerTriggerIndicator />
                  </Calendar.YearPickerTrigger>
                  <Calendar.NavButton slot="previous" />
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
                <Calendar.YearPickerGrid>
                  <Calendar.YearPickerGridBody>
                    {({ year }) => <Calendar.YearPickerCell year={year} />}
                  </Calendar.YearPickerGridBody>
                </Calendar.YearPickerGrid>
              </Calendar>
            </DatePicker.Popover>
          </DatePicker>

          <FormSelectField
            label={t("clinic.patient.fields.gender")}
            value={formData.gender}
            onChange={(v) => handleChange("gender", v)}
            options={[
              {
                value: "Unspecified",
                label: t("clinic.patient.gender.unspecified"),
              },
              { value: "Female", label: t("clinic.patient.gender.female") },
              { value: "Male", label: t("clinic.patient.gender.male") },
            ]}
          />
          <FormTextField
            label={t("clinic.patient.fields.placeOfBirth")}
            value={formData.placeOfBirth}
            onChange={(v) => handleChange("placeOfBirth", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.fatherName")}
            value={formData.fatherName}
            onChange={(v) => handleChange("fatherName", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.occupation")}
            value={formData.occupation}
            onChange={(v) => handleChange("occupation", v)}
          />
        </div>
      </section>

      <section>
        <h2 className="mb-4 text-sm font-semibold text-foreground">
          {t("clinic.patient.sections.tags")}
        </h2>
        <div className="flex flex-col gap-3">
          <div className="flex items-end gap-2">
            <TextField className="min-w-0 flex-1" variant="secondary">
              <Label>{t("clinic.patient.fields.tags")}</Label>
              <Input
                value={tagDraft}
                placeholder={t("clinic.patient.tags.placeholder")}
                onChange={(event) => setTagDraft(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    addTag();
                  }
                }}
              />
            </TextField>
            <Tooltip>
              <Button
                type="button"
                isIconOnly
                variant="tertiary"
                aria-label={t("clinic.patient.tags.add")}
                onPress={addTag}
              >
                <FaIcon icon="plus" aria-hidden="true" />
              </Button>
              <Tooltip.Content>{t("clinic.patient.tags.add")}</Tooltip.Content>
            </Tooltip>
          </div>
          {formData.tags.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {formData.tags.map((tag) => (
                <Chip key={tag} size="sm" variant="soft">
                  <Chip.Label>{tag}</Chip.Label>
                  <Tooltip>
                    <Button
                      type="button"
                      isIconOnly
                      size="sm"
                      variant="ghost"
                      aria-label={t("clinic.patient.tags.remove", { tag })}
                      onPress={() => removeTag(tag)}
                    >
                      <FaIcon icon="xmark" aria-hidden="true" />
                    </Button>
                    <Tooltip.Content>{t("clinic.patient.tags.remove", { tag })}</Tooltip.Content>
                  </Tooltip>
                </Chip>
              ))}
            </div>
          )}
        </div>
      </section>

      <section>
        <h2 className="mb-4 text-sm font-semibold text-foreground">
          {t("clinic.patient.sections.contact")}
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormTextField
            label={t("clinic.patient.fields.phone")}
            value={formData.phone}
            onChange={(v) => handleChange("phone", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.email")}
            value={formData.email}
            onChange={(v) => handleChange("email", v)}
            error={errors.email}
          />
          <FormTextField
            label={t("clinic.patient.fields.address")}
            value={formData.address}
            onChange={(v) => handleChange("address", v)}
            className="sm:col-span-2"
          />
          <FormTextField
            label={t("clinic.patient.fields.emergencyContact")}
            value={formData.emergencyContactName}
            onChange={(v) => handleChange("emergencyContactName", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.emergencyPhone")}
            value={formData.emergencyContactPhone}
            onChange={(v) => handleChange("emergencyContactPhone", v)}
          />
        </div>
      </section>

      <section>
        <h2 className="mb-4 text-sm font-semibold text-foreground">
          {t("clinic.patient.sections.medical")}
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormTextField
            label={t("clinic.patient.fields.heightCm")}
            value={formData.height}
            onChange={(v) => handleChange("height", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.weightKg")}
            value={formData.weight}
            onChange={(v) => handleChange("weight", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.allergies")}
            value={formData.allergies}
            onChange={(v) => handleChange("allergies", v)}
            className="sm:col-span-2"
          />
          <FormTextField
            label={t("clinic.patient.fields.currentMedications")}
            value={formData.currentMedications}
            onChange={(v) => handleChange("currentMedications", v)}
            className="sm:col-span-2"
          />
          <FormTextField
            label={t("clinic.patient.fields.clinicalNotes")}
            value={formData.clinicalNotes}
            onChange={(v) => handleChange("clinicalNotes", v)}
            className="sm:col-span-2"
          />
        </div>
      </section>

      <section>
        <h2 className="mb-4 text-sm font-semibold text-foreground">
          {t("clinic.patient.sections.insurance")}
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormTextField
            label={t("clinic.patient.fields.insuranceProvider")}
            value={formData.insuranceProvider}
            onChange={(v) => handleChange("insuranceProvider", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.referrer")}
            value={formData.referrer}
            onChange={(v) => handleChange("referrer", v)}
          />
          <FormTextField
            label={t("clinic.patient.fields.visitReason")}
            value={formData.visitReason}
            onChange={(v) => handleChange("visitReason", v)}
            className="sm:col-span-2"
          />
        </div>
      </section>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onPress={onCancel}>
          {t("clinic.patient.edit.cancel")}
        </Button>
        <Button variant="primary" type="submit" isPending={isPending}>
          {t("clinic.patient.edit.save")}
        </Button>
      </div>
    </form>
  );
}

function FormTextField({
  label,
  value,
  onChange,
  error,
  isRequired,
  className,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  isRequired?: boolean;
  className?: string;
}) {
  return (
    <TextField
      isInvalid={!!error}
      isRequired={isRequired}
      className={className}
      variant="secondary"
    >
      <Label>{label}</Label>
      <Input value={value} onChange={(e) => onChange(e.target.value)} />
      {error && <FieldError>{error}</FieldError>}
    </TextField>
  );
}

function FormSelectField({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
}) {
  const { t } = useLocalizedText();

  return (
    <NativeSelect fullWidth variant="secondary">
      <Label>{label}</Label>
      <NativeSelect.Trigger
        value={value}
        onChange={(e) => onChange(e.target.value)}
      >
        <NativeSelect.Option value="">
          {t("clinic.patient.edit.selectPlaceholder")}
        </NativeSelect.Option>
        {options.map((option) => (
          <NativeSelect.Option key={option.value} value={option.value}>
            {option.label}
          </NativeSelect.Option>
        ))}
        <NativeSelect.Indicator />
      </NativeSelect.Trigger>
    </NativeSelect>
  );
}
