"use client";

import { useCallback, useState } from "react";
import { Button, Input, Label, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import type { AdvancedSearchCriteria } from "../types";
import { useLocalizedText } from "@/localization";
import { LocalizedDateField } from "@/components/forms/LocalizedDateField";

interface AdvancedSearchFormProps {
  onSearch: (criteria: AdvancedSearchCriteria) => void;
  isPending: boolean;
}

export function AdvancedSearchForm({
  onSearch,
  isPending,
}: AdvancedSearchFormProps) {
  const [name, setName] = useState("");
  const [identificationCode, setIdentificationCode] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const { t } = useLocalizedText();

  const handleSubmit = useCallback(() => {
    onSearch({ name, identificationCode, dateOfBirth, phone, email });
  }, [name, identificationCode, dateOfBirth, phone, email, onSearch]);

  const handleClear = useCallback(() => {
    setName("");
    setIdentificationCode("");
    setDateOfBirth("");
    setPhone("");
    setEmail("");
  }, []);

  const hasValues = name || identificationCode || dateOfBirth || phone || email;

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("clinic.patient.advancedSearch.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-4">
        <TextField>
          <Label>{t("clinic.patient.advancedSearch.nameLabel")}</Label>
          <Input
            placeholder={t("clinic.patient.advancedSearch.namePlaceholder")}
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </TextField>

        <TextField>
          <Label>{t("clinic.patient.advancedSearch.idCodeLabel")}</Label>
          <Input
            placeholder={t("clinic.patient.advancedSearch.idCodePlaceholder")}
            value={identificationCode}
            onChange={(e) => setIdentificationCode(e.target.value)}
          />
        </TextField>

        <LocalizedDateField
          label={t("clinic.patient.advancedSearch.dobLabel")}
          value={dateOfBirth}
          onChange={setDateOfBirth}
        />

        <TextField>
          <Label>{t("clinic.patient.advancedSearch.phoneLabel")}</Label>
          <Input
            placeholder={t("clinic.patient.advancedSearch.phonePlaceholder")}
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
          />
        </TextField>

        <TextField>
          <Label>{t("clinic.patient.advancedSearch.emailLabel")}</Label>
          <Input
            placeholder={t("clinic.patient.advancedSearch.emailPlaceholder")}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </TextField>

        <div className="flex gap-2">
          <Button
            variant="primary"
            isPending={isPending}
            isDisabled={!hasValues}
            onPress={handleSubmit}
          >
            {t("clinic.patient.advancedSearch.searchAction")}
          </Button>
          {hasValues && (
            <Button variant="ghost" onPress={handleClear}>
              {t("clinic.patient.advancedSearch.clearAction")}
            </Button>
          )}
        </div>
      </Widget.Content>
    </Widget>
  );
}
