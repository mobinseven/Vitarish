"use client";

import { useCallback, useEffect, useState } from "react";
import { TextField, Label, Input, Button } from "@heroui/react";

interface PatientSearchBarProps {
  onSearch: (search: string) => void;
}

export function PatientSearchBar({ onSearch }: PatientSearchBarProps) {
  const [value, setValue] = useState("");

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
        <Label>Search patients</Label>
        <Input
          placeholder="Name, ID code, phone, or email..."
          value={value}
          onChange={(e) => setValue(e.target.value)}
        />
      </TextField>
      {value && (
        <Button variant="ghost" onPress={handleClear}>
          Clear
        </Button>
      )}
    </div>
  );
}
