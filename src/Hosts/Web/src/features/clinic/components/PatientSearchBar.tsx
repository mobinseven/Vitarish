"use client";

import { useCallback, useEffect, useState } from "react";
import { TextField, Label, Input, Button } from "@heroui/react";
import { useLocalizedText } from "@/localization";

interface PatientSearchBarProps {
  onSearch: (search: string) => void;
}

export function PatientSearchBar({ onSearch }: PatientSearchBarProps) {
  const [value, setValue] = useState("");
  const { t } = useLocalizedText();

  useEffect(() => {
    const timer = setTimeout(() => {
      onSearch(value);
    }, 300);

    return () => clearTimeout(timer);
  }, [value, onSearch]);

  const handleClear = useCallback(() => {
    setValue("");
    onSearch("");
  }, [onSearch]);

  return (
    <div className="flex items-end gap-2">
      <TextField className="flex-1">
        <Label>{t("clinic.patient.search.label")}</Label>
        <Input
          placeholder={t("clinic.patient.search.placeholder")}
          value={value}
          onChange={(e) => setValue(e.target.value)}
        />
      </TextField>
      {value && (
        <Button variant="ghost" onPress={handleClear}>
          {t("clinic.patient.search.clear")}
        </Button>
      )}
    </div>
  );
}
