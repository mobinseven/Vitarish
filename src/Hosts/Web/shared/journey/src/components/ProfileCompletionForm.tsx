"use client";

import {
  Alert,
  Button,
  FieldError,
  Form,
  Input,
  Label,
  TextField,
  Tooltip,
} from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { useCompleteProfile } from "../hooks/useCompleteProfile";

interface ProfileCompletionFormProps {
  /** The signed-in user; undefined while the session is still loading. */
  userId: string | undefined;
}

export function ProfileCompletionForm({ userId }: ProfileCompletionFormProps) {
  const { values, errors, setField, isSubmitting, canSubmit, submit } =
    useCompleteProfile(userId);
  const { t } = useLocalizedText();

  return (
    <Form
      onSubmit={(event) => {
        event.preventDefault();
        submit();
      }}
      className="flex flex-col gap-4"
    >
      {errors.form ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>{errors.form}</Alert.Content>
        </Alert>
      ) : null}
      <TextField
        name="firstName"
        isRequired
        isInvalid={Boolean(errors.firstName)}
      >
        <Label>{t("journey.gates.profile.firstName")}</Label>
        <Input
          value={values.firstName}
          onChange={(event) => setField("firstName", event.target.value)}
          placeholder={t("journey.gates.profile.firstNamePlaceholder")}
          autoComplete="given-name"
        />
        {errors.firstName ? <FieldError>{errors.firstName}</FieldError> : null}
      </TextField>
      <TextField
        name="lastName"
        isRequired
        isInvalid={Boolean(errors.lastName)}
      >
        <Label>{t("journey.gates.profile.lastName")}</Label>
        <Input
          value={values.lastName}
          onChange={(event) => setField("lastName", event.target.value)}
          placeholder={t("journey.gates.profile.lastNamePlaceholder")}
          autoComplete="family-name"
        />
        {errors.lastName ? <FieldError>{errors.lastName}</FieldError> : null}
      </TextField>
      <Tooltip>
        <Button
          type="submit"
          isDisabled={!canSubmit}
          isPending={isSubmitting}
          className="w-full"
        >
          <FaIcon icon="floppy-disk" aria-hidden="true" />
          {t("journey.gates.profile.saveAndContinue")}
        </Button>
        <Tooltip.Content>{t("journey.gates.profile.saveAndContinue")}</Tooltip.Content>
      </Tooltip>
    </Form>
  );
}
