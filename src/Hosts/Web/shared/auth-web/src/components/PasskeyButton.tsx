"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { usePasskeyLogin } from "../hooks/usePasskeyLogin";
import { useLocalizedText } from "@vitarish/web-localization";
import { StatusAlert } from "@vitarish/web-ui";

interface PasskeyButtonProps {
  returnUrl?: string;
}

export function PasskeyButton({ returnUrl }: PasskeyButtonProps) {
  const { login, isLoading, errorMessage } = usePasskeyLogin();
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
    [handleLogin]
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
        <svg
          className="w-5 h-5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M12 11c0 3.517-1.009 6.799-2.753 9.571m-3.44-2.04l.054-.09A13.916 13.916 0 008 11a4 4 0 118 0c0 1.017-.07 2.019-.203 3m-2.118 6.844A21.88 21.88 0 0015.171 17m3.839 1.132c.645-2.266.99-4.659.99-7.132A8 8 0 008 4.07M3 15.364c.64-1.319 1-2.8 1-4.364 0-1.457.39-2.823 1.07-4"
          />
        </svg>
        {t("auth.passkey.signIn")}
      </Button>
    </Form>
  );
}
