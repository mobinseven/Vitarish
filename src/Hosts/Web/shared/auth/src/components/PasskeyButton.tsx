"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { usePasskeyLogin } from "../hooks/usePasskeyLogin";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon, StatusAlert } from "@vitarish/ui";
import type { AuthNavigationOptions } from "../hooks/navigation";

interface PasskeyButtonProps extends AuthNavigationOptions {
  returnUrl?: string;
}

export function PasskeyButton({
  returnUrl,
  navigate,
  returnUrlSearchParams,
}: PasskeyButtonProps) {
  const { login, isLoading, errorMessage } = usePasskeyLogin({
    navigate,
    returnUrlSearchParams,
  });
  const { t } = useLocalizedText();
  const [userNameOrEmail, setUserNameOrEmail] = useState("");

  const handleLogin = useCallback(() => {
    login(userNameOrEmail || undefined, returnUrl);
  }, [login, userNameOrEmail, returnUrl]);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      handleLogin();
    },
    [handleLogin],
  );

  return (
    <Form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusAlert className="mb-4" message={errorMessage} />}
      <TextField name="username" isDisabled={isLoading}>
        <Label>{t("auth.login.usernameOrEmailOptionalLabel")}</Label>
        <Input
          placeholder={t("auth.login.usernameOrEmailPlaceholder")}
          value={userNameOrEmail}
          onChange={(e) => setUserNameOrEmail(e.target.value)}
          autoComplete="username"
        />
      </TextField>
      <Button
        type="submit"
        variant="secondary"
        isPending={isLoading}
        className="w-full"
      >
        <FaIcon aria-hidden="true" className="text-xl" icon="fingerprint" />
        {t("auth.passkey.signIn")}
      </Button>
    </Form>
  );
}
