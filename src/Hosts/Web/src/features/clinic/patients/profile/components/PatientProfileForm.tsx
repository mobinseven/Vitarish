"use client";

import { useCallback, useState } from "react";
import { Card, Button, TextField, Label, Input, Select, ListBox, FieldError } from "@heroui/react";
import type { PatientProfileFormData, PatientProfileFormErrors } from "../models/PatientProfileForm";
import { useLocalizedText } from "@/localization";

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

  const handleChange = useCallback(
    (field: keyof PatientProfileFormData, value: string) => {
      setFormData((prev) => ({ ...prev, [field]: value }));
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    },
    []
  );

  const handleSubmit = useCallback(() => {
    const newErrors: PatientProfileFormErrors = {};
    if (!formData.firstName.trim()) newErrors.firstName = t("clinic.patient.edit.validation.firstNameRequired");
    if (!formData.lastName.trim()) newErrors.lastName = t("clinic.patient.edit.validation.lastNameRequired");
    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = t("clinic.patient.edit.validation.invalidEmail");
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    onSubmit(formData);
  }, [formData, onSubmit, t]);

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.personal")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
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
          />
          <FormDateField
            label={t("clinic.patient.fields.dateOfBirth")}
            value={formData.dateOfBirth}
            onChange={(v) => handleChange("dateOfBirth", v)}
          />
          <FormSelectField
            label={t("clinic.patient.fields.gender")}
            value={formData.gender}
            onChange={(v) => handleChange("gender", v)}
            options={[
              { value: "Unspecified", label: t("clinic.patient.gender.unspecified") },
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
        </Card.Content>
      </Card>

      <Card>
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.contact")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
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
          <div className="sm:col-span-2">
            <FormTextField
              label={t("clinic.patient.fields.address")}
              value={formData.address}
              onChange={(v) => handleChange("address", v)}
            />
          </div>
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
        </Card.Content>
      </Card>

      <Card>
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.medical")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <FormDateField
            label={t("clinic.patient.fields.dateOfSurgery")}
            value={formData.dateOfSurgery}
            onChange={(v) => handleChange("dateOfSurgery", v)}
          />
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
          <div className="sm:col-span-2">
            <FormTextField
              label={t("clinic.patient.fields.allergies")}
              value={formData.allergies}
              onChange={(v) => handleChange("allergies", v)}
            />
          </div>
          <div className="sm:col-span-2">
            <FormTextField
              label={t("clinic.patient.fields.currentMedications")}
              value={formData.currentMedications}
              onChange={(v) => handleChange("currentMedications", v)}
            />
          </div>
          <div className="sm:col-span-2">
            <FormTextField
              label={t("clinic.patient.fields.clinicalNotes")}
              value={formData.clinicalNotes}
              onChange={(v) => handleChange("clinicalNotes", v)}
            />
          </div>
        </Card.Content>
      </Card>

      <Card>
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patient.sections.insurance")}</h2>
        </Card.Header>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
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
          <div className="sm:col-span-2">
            <FormTextField
              label={t("clinic.patient.fields.visitReason")}
              value={formData.visitReason}
              onChange={(v) => handleChange("visitReason", v)}
            />
          </div>
        </Card.Content>
      </Card>

      <div className="flex justify-end gap-2">
        <Button variant="ghost" onPress={onCancel}>
          {t("clinic.patient.edit.cancel")}
        </Button>
        <Button variant="primary" isPending={isPending} onPress={handleSubmit}>
          {t("clinic.patient.edit.save")}
        </Button>
      </div>
    </div>
  );
}

function FormTextField({
  label,
  value,
  onChange,
  error,
  isRequired,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  isRequired?: boolean;
}) {
  return (
    <TextField isInvalid={!!error}>
      <Label>
        {label}
        {isRequired && <span className="text-danger ms-1">*</span>}
      </Label>
      <Input value={value} onChange={(e) => onChange(e.target.value)} />
      {error && <FieldError>{error}</FieldError>}
    </TextField>
  );
}

function FormDateField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <TextField>
      <Label>{label}</Label>
      <Input type="date" value={value} onChange={(e) => onChange(e.target.value)} />
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
  return (
    <Select
      selectedKey={value}
      onSelectionChange={(key) => onChange(String(key))}
    >
      <Label>{label}</Label>
      <Select.Trigger>
        <Select.Value />
        <Select.Indicator />
      </Select.Trigger>
      <Select.Popover>
        <ListBox>
          {options.map((opt) => (
            <ListBox.Item key={opt.value} id={opt.value} textValue={opt.label}>
              {opt.label}
              <ListBox.ItemIndicator />
            </ListBox.Item>
          ))}
        </ListBox>
      </Select.Popover>
    </Select>
  );
}
