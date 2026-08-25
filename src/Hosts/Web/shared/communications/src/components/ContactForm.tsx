"use client";

import type { FormEvent } from "react";
import {
  Alert,
  Button,
  FieldError,
  Form,
  Input,
  Label,
  TextArea,
  TextField,
  Tooltip,
} from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import {
  type ContactFormSubmit,
  useContactForm,
} from "../hooks/useContactForm";

export interface ContactFormOption {
  id: string;
  label: string;
}

export interface ContactFormConversation {
  prefix: string;
  between: string;
  suffix: string;
  goalLabel: string;
  needLabel: string;
  goals: ContactFormOption[];
  needs: ContactFormOption[];
}

export interface ContactFormProps {
  onSubmit: ContactFormSubmit;
  context?: string;
  className?: string;
  conversation?: ContactFormConversation;
}

export function ContactForm({
  onSubmit,
  context,
  className,
  conversation,
}: ContactFormProps) {
  const { t } = useLocalizedText();
  const form = useContactForm({
    onSubmit,
    context,
    goal: conversation?.goals[0]?.id ?? "general",
    need: conversation?.needs[0]?.id ?? "inquiry",
  });
  const isSubmitting = form.status === "submitting";
  const requiredMessage = t("communications.contactForm.validation.required");

  const validateRequired = (value: string) =>
    value.trim().length === 0 ? requiredMessage : null;

  const validateEmail = (value: string) => {
    if (value.trim().length === 0) {
      return requiredMessage;
    }
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
      ? null
      : t("communications.contactForm.validation.email");
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    void form.submit();
  };

  return (
    <Form
      aria-label={t("communications.contactForm.label")}
      className={className}
      validationBehavior="native"
      onSubmit={handleSubmit}
    >
      {conversation ? (
        <div className="contact-form__conversation">
          <span>{conversation.prefix}</span>
          <span className="contact-form__inline-select">
            <select
              aria-label={conversation.goalLabel}
              value={form.values.goal}
              onChange={(event) => form.setGoal(event.currentTarget.value)}
            >
              {conversation.goals.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
            <i className="fa-light fa-chevron-down" aria-hidden="true" />
          </span>
          <span>{conversation.between}</span>
          <span className="contact-form__inline-select">
            <select
              aria-label={conversation.needLabel}
              value={form.values.need}
              onChange={(event) => form.setNeed(event.currentTarget.value)}
            >
              {conversation.needs.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
            <i className="fa-light fa-chevron-down" aria-hidden="true" />
          </span>
          <span>{conversation.suffix}</span>
        </div>
      ) : null}

      <div className="contact-form__details">
        <TextField
          className="contact-form__field"
          fullWidth
          isDisabled={isSubmitting}
          isRequired
          name="name"
          validate={validateRequired}
          value={form.values.name}
          onChange={form.setName}
        >
          <Label>{t("communications.contactForm.fields.name")}</Label>
          <Input className="contact-form__input" autoComplete="name" />
          <FieldError />
        </TextField>

        <TextField
          className="contact-form__field"
          fullWidth
          isDisabled={isSubmitting}
          isRequired
          name="email"
          type="email"
          validate={validateEmail}
          value={form.values.email}
          onChange={form.setEmail}
        >
          <Label>{t("communications.contactForm.fields.email")}</Label>
          <Input
            className="contact-form__input"
            autoComplete="email"
            inputMode="email"
          />
          <FieldError />
        </TextField>

        <TextField
          className="contact-form__field contact-form__message"
          fullWidth
          isDisabled={isSubmitting}
          isRequired
          name="message"
          validate={validateRequired}
          value={form.values.message}
          onChange={form.setMessage}
        >
          <Label>{t("communications.contactForm.fields.message")}</Label>
          <TextArea className="contact-form__input" rows={5} />
          <FieldError />
        </TextField>
      </div>

      {form.status === "succeeded" ? (
        <Alert status="success">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {t("communications.contactForm.success.title")}
            </Alert.Title>
            <Alert.Description>
              {t("communications.contactForm.success.description")}
            </Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}

      {form.status === "failed" ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {t("communications.contactForm.failure.title")}
            </Alert.Title>
            <Alert.Description>
              {t("communications.contactForm.failure.description")}
            </Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}

      <Tooltip>
        <Button
          fullWidth
          isDisabled={isSubmitting}
          isPending={isSubmitting}
          type="submit"
          variant="primary"
        >
          <span>
            {t(
              isSubmitting
                ? "communications.contactForm.actions.submitting"
                : "communications.contactForm.actions.submit",
            )}
          </span>
          <i className="fa-light fa-arrow-up-right" aria-hidden="true" />
        </Button>
        <Tooltip.Content>
          {t(
            isSubmitting
              ? "communications.contactForm.actions.pendingHint"
              : "communications.contactForm.actions.submitHint",
          )}
        </Tooltip.Content>
      </Tooltip>
    </Form>
  );
}
