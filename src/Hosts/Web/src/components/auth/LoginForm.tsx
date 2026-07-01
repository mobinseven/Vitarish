"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { usePasswordLogin } from "@/hooks/auth/usePasswordLogin";
import { useLocalizedText } from "@/localization";
import { StatusMessage } from "./StatusMessage";

interface LoginFormProps {
  returnUrl?: string;
}

export function LoginForm({ returnUrl }: LoginFormProps) {
  const { login, isLoading, errorMessage } = usePasswordLogin();
  const { t } = useLocalizedText();
  const [userNameOrEmail, setUserNameOrEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      login(userNameOrEmail, password, returnUrl);
    },
    [userNameOrEmail, password, returnUrl, login]
  );

  return (
    <Form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {errorMessage && <StatusMessage message={errorMessage} />}
      <TextField name="username" isRequired isDisabled={isLoading}>
        <Label>{t("auth.login.usernameOrEmailLabel")}</Label>
        <Input
          placeholder={t("auth.login.usernameOrEmailPlaceholder")}
          value={userNameOrEmail}
          onChange={(e) => setUserNameOrEmail(e.target.value)}
          autoComplete="username"
        />
      </TextField>
      <TextField name="password" type="password" isRequired isDisabled={isLoading}>
        <Label>{t("auth.login.passwordLabel")}</Label>
        <Input
          placeholder={t("auth.login.passwordPlaceholder")}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
        />
      </TextField>
      <Button
        type="submit"
        isPending={isLoading}
        className="w-full"
      >
        {t("auth.login.signInAction")}
      </Button>
    </Form>
  );
}
