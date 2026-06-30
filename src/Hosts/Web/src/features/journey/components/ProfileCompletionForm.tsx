"use client";

import { Alert, Button, FieldError, Form, Input, Label, TextField } from "@heroui/react";
import { useCompleteProfile } from "../hooks/useCompleteProfile";

export function ProfileCompletionForm() {
  const { values, errors, setField, isSubmitting, canSubmit } = useCompleteProfile();

  return (
    <Form onSubmit={(event) => event.preventDefault()} className="flex flex-col gap-4">
      {errors.form ? (
        <Alert status="warning">
          <Alert.Indicator />
          <Alert.Content>{errors.form}</Alert.Content>
        </Alert>
      ) : null}
      <TextField name="firstName" isRequired isInvalid={Boolean(errors.firstName)}>
        <Label>First name</Label>
        <Input
          value={values.firstName}
          onChange={(event) => setField("firstName", event.target.value)}
          placeholder="Enter your first name"
          autoComplete="given-name"
        />
        {errors.firstName ? <FieldError>{errors.firstName}</FieldError> : null}
      </TextField>
      <TextField name="lastName" isRequired isInvalid={Boolean(errors.lastName)}>
        <Label>Last name</Label>
        <Input
          value={values.lastName}
          onChange={(event) => setField("lastName", event.target.value)}
          placeholder="Enter your last name"
          autoComplete="family-name"
        />
        {errors.lastName ? <FieldError>{errors.lastName}</FieldError> : null}
      </TextField>
      <Button type="submit" isDisabled={!canSubmit} isPending={isSubmitting} className="w-full">
        Save and continue
      </Button>
    </Form>
  );
}
