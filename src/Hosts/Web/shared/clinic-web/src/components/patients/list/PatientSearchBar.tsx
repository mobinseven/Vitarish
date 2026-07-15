"use client";

import { useEffect, useState } from "react";
import { SearchField } from "@heroui/react";
import { useLocalizedText } from "../../../localization/index";

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

  return (
    <SearchField
      aria-label={t("clinic.patient.search.label")}
      className="min-w-0 w-full"
      value={value}
      onChange={setValue}
    >
      <SearchField.Group>
        <SearchField.SearchIcon />
        <SearchField.Input
          placeholder={t("clinic.patient.search.placeholder")}
        />
        <SearchField.ClearButton aria-label={t("clinic.patient.search.clear")} />
      </SearchField.Group>
    </SearchField>
  );
}
