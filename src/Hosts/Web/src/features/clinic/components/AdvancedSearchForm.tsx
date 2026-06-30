"use client";

import { useCallback, useState } from "react";
import { Card, TextField, Label, Input, Button } from "@heroui/react";
import type { AdvancedSearchCriteria } from "../types";

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
        <h3 className="text-lg font-semibold">Advanced Search</h3>
      </Card.Header>
      <Card.Content className="flex flex-col gap-3">
        <TextField>
          <Label>Name</Label>
          <Input
            placeholder="Patient name..."
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </TextField>

        <TextField>
          <Label>Identification Code</Label>
          <Input
            placeholder="ID code..."
            value={identificationCode}
            onChange={(e) => setIdentificationCode(e.target.value)}
          />
        </TextField>

        <TextField>
          <Label>Date of Birth</Label>
          <Input
            type="date"
            value={dateOfBirth}
            onChange={(e) => setDateOfBirth(e.target.value)}
          />
        </TextField>

        <TextField>
          <Label>Phone</Label>
          <Input
            placeholder="Phone number..."
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
          />
        </TextField>

        <TextField>
          <Label>Email</Label>
          <Input
            placeholder="Email address..."
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
            Search
          </Button>
          {hasValues && (
            <Button variant="ghost" onPress={handleClear}>
              Clear
            </Button>
          )}
        </div>
      </Card.Content>
    </Card>
  );
}
