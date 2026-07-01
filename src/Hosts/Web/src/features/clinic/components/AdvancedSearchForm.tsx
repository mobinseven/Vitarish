"use client";

import { useCallback, useState } from "react";
import { Card, TextField, Label, Input, Button } from "@heroui/react";
import type { AdvancedSearchCriteria } from "../types";
import { useLocalizedText } from "@/localization";

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
    <Card className="glass-card p-4">
      <Card.Header>
        <h3 className="text-lg font-semibold">{t("clinic.patient.advancedSearch.title")}</h3>
      </Card.Header>
      <Card.Content className="flex flex-col gap-4">
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

        <TextField>
          <Label>{t("clinic.patient.advancedSearch.dobLabel")}</Label>
          <Input
            type="date"
            value={dateOfBirth}
            onChange={(e) => setDateOfBirth(e.target.value)}
          />
        </TextField>

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
      </Card.Content>
    </Card>
  );
}
