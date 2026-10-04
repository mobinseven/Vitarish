"use client";

import { useEffect, useState } from "react";
import { Button, Form, Input, Label, TextField, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon, MobileNumberField, StatusAlert } from "@vitarish/ui";
import { usePasswordRecovery } from "../hooks/usePasswordRecovery";
import type { PasswordRecoveryChannel } from "../auth/passwordRecoveryChannel";

export function PasswordRecoveryForm() {
  const { t } = useLocalizedText();
  const recovery = usePasswordRecovery();
  const [channel, setChannel] = useState<PasswordRecoveryChannel>("email");
  const [contact, setContact] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [passwordMismatch, setPasswordMismatch] = useState(false);
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  if (recovery.isComplete) {
    return (
      <div className="flex flex-col gap-4">
        <p role="status">{t("auth.recovery.success")}</p>
      </div>
    );
  }

  if (recovery.hasChallenge) {
    return (
      <Form
        onSubmit={(event) => {
          event.preventDefault();
          if (newPassword !== confirmPassword) {
            setPasswordMismatch(true);
            return;
          }
          setPasswordMismatch(false);
          void recovery.complete(code, newPassword);
        }}
        className="flex flex-col gap-4"
      >
        <p role="status">{t("auth.recovery.codeSentNotice")}</p>
        {recovery.errorMessage && <StatusAlert message={recovery.errorMessage} />}
        {passwordMismatch && <p role="alert">{t("auth.recovery.passwordMismatch")}</p>}
        <TextField name="recovery-code" isRequired isDisabled={recovery.isLoading}>
          <Label>{t("auth.recovery.codeLabel")}</Label>
          <Input
            autoComplete="one-time-code"
            inputMode="numeric"
            value={code}
            onChange={(event) => setCode(event.target.value)}
          />
        </TextField>
        <TextField name="new-password" type="password" isRequired isDisabled={recovery.isLoading}>
          <Label>{t("auth.recovery.newPasswordLabel")}</Label>
          <Input
            autoComplete="new-password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
          />
        </TextField>
        <TextField name="confirm-password" type="password" isRequired isDisabled={recovery.isLoading}>
          <Label>{t("auth.recovery.confirmPasswordLabel")}</Label>
          <Input
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(event) => setConfirmPassword(event.target.value)}
          />
        </TextField>
        <Tooltip>
          <Button type="submit" isPending={recovery.isLoading} className="w-full">
            <FaIcon icon="key" aria-hidden="true" />
            {t("auth.recovery.resetPasswordAction")}
          </Button>
          <Tooltip.Content>{t("auth.recovery.resetPasswordAction")}</Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button type="button" variant="tertiary" isDisabled={recovery.isLoading} onPress={recovery.startOver}>
            <FaIcon icon="arrow-rotate-left" aria-hidden="true" />
            {t("auth.recovery.startOverAction")}
          </Button>
          <Tooltip.Content>{t("auth.recovery.startOverAction")}</Tooltip.Content>
        </Tooltip>
      </Form>
    );
  }

  return (
    <Form
      method="post"
      onSubmit={(event) => {
        event.preventDefault();
        void recovery.begin(channel, contact);
      }}
      className="flex flex-col gap-4"
    >
      <p>{t("auth.recovery.instructions")}</p>
      {recovery.errorMessage && <StatusAlert message={recovery.errorMessage} />}
      <div className="flex gap-2" role="group" aria-label={t("auth.recovery.channelLabel")}>
        <Tooltip>
          <Button
            type="button"
            variant={channel === "email" ? "primary" : "tertiary"}
            aria-pressed={channel === "email"}
            onPress={() => setChannel("email")}
          >
            <FaIcon icon="envelope" aria-hidden="true" />
            {t("auth.recovery.emailChannel")}
          </Button>
          <Tooltip.Content>{t("auth.recovery.emailChannel")}</Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button
            type="button"
            variant={channel === "sms" ? "primary" : "tertiary"}
            aria-pressed={channel === "sms"}
            onPress={() => setChannel("sms")}
          >
            <FaIcon icon="mobile-screen-button" aria-hidden="true" />
            {t("auth.recovery.smsChannel")}
          </Button>
          <Tooltip.Content>{t("auth.recovery.smsChannel")}</Tooltip.Content>
        </Tooltip>
      </div>
      {channel === "email" ? (
        <TextField name="recovery-contact" type="email" isRequired isDisabled={recovery.isLoading}>
          <Label>{t("auth.recovery.emailLabel")}</Label>
          <Input
            autoComplete="email"
            value={contact}
            onChange={(event) => setContact(event.target.value)}
          />
        </TextField>
      ) : (
        <MobileNumberField
          name="recovery-contact"
          label={t("auth.recovery.phoneLabel")}
          hint={t("auth.recovery.phoneHint")}
          value={contact}
          onChange={setContact}
          isRequired
          isDisabled={recovery.isLoading}
        />
      )}
      <Tooltip>
        <Button type="submit" isDisabled={!isHydrated || recovery.isLoading} isPending={recovery.isLoading} className="w-full">
          <FaIcon icon="paper-plane" aria-hidden="true" />
          {t("auth.recovery.sendCodeAction")}
        </Button>
        <Tooltip.Content>{t("auth.recovery.sendCodeAction")}</Tooltip.Content>
      </Tooltip>
    </Form>
  );
}
