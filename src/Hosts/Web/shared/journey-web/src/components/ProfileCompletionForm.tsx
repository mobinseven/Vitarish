"use client";

import { Alert, Button, FieldError, Form, Input, Label, TextField } from "@heroui/react";
import { useLocalizedText } from "@vitarish/web-localization";
import { useCompleteProfile } from "../hooks/useCompleteProfile";

export function ProfileCompletionForm() {
  const { values, errors, setField, isSubmitting, canSubmit } = useCompleteProfile();
  const { t } = useLocalizedText();

  return (
    <Form onSubmit={(event) => event.preventDefault()} className="flex flex-col gap-4">
      {errors.form ? (
        <Alert status="warning">
          <Alert.Indicator />
          <Alert.Content>{errors.form}</Alert.Content>
        </Alert>
      ) : null}
      <TextField name="firstName" isRequired isInvalid={Boolean(errors.firstName)}>
        <Label>{t("journey.gates.profile.firstName")}</Label>
        <Input
          value={values.firstName}
          onChange={(event) => setField("firstName", event.target.value)}
          placeholder={t("journey.gates.profile.firstNamePlaceholder")}
          autoComplete="given-name"
        />
        {errors.firstName ? <FieldError>{errors.firstName}</FieldError> : null}
      </TextField>
      <TextField name="lastName" isRequired isInvalid={Boolean(errors.lastName)}>
        <Label>{t("journey.gates.profile.lastName")}</Label>
        <Input
          value={values.lastName}
          onChange={(event) => setField("lastName", event.target.value)}
          placeholder={t("journey.gates.profile.lastNamePlaceholder")}
          autoComplete="family-name"
        />
        {errors.lastName ? <FieldError>{errors.lastName}</FieldError> : null}
      </TextField>
      <Button type="submit" isDisabled={!canSubmit} isPending={isSubmitting} className="w-full">
        {t("journey.gates.profile.saveAndContinue")}
      </Button>
    </Form>
  );
}
