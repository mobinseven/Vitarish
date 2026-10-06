"use client";

import { useState, useCallback, useEffect } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
import { usePasswordLogin } from "../hooks/usePasswordLogin";
import { useLocalizedText } from "@vitarish/localization";
import { StatusAlert } from "@vitarish/ui";
import type { AuthNavigationOptions } from "../hooks/navigation";

interface LoginFormProps extends AuthNavigationOptions {
  returnUrl?: string;
  forgotPasswordHref?: string;
}

export function LoginForm({
  returnUrl,
  forgotPasswordHref,
  navigate,
  returnUrlSearchParams,
}: LoginFormProps) {
  const { login, completeOtp, requiresOtp, isLoading, error, errorMessage } = usePasswordLogin({
    navigate,
    returnUrlSearchParams,
  });
  const { t } = useLocalizedText();
  const displayedError = error?.code === "account_disabled" ? t("auth.login.accountDisabled") : errorMessage;
  const [userNameOrEmail, setUserNameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      login(userNameOrEmail, password, returnUrl);
    },
    [userNameOrEmail, password, returnUrl, login],
  );

  const handleOtpSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      completeOtp(otpCode);
    },
    [completeOtp, otpCode],
  );

  return (
    <Form
      method="post"
      onSubmit={requiresOtp ? handleOtpSubmit : handleSubmit}
      className="flex flex-col gap-4"
    >
      {displayedError && <StatusAlert className="mb-4" message={displayedError} />}
      {!requiresOtp && (
        <TextField name="username" isRequired isDisabled={isLoading}>
          <Label>{t("auth.login.usernameOrEmailLabel")}</Label>
          <Input
            placeholder={t("auth.login.usernameOrEmailPlaceholder")}
            value={userNameOrEmail}
            onChange={(e) => setUserNameOrEmail(e.target.value)}
            autoComplete="username"
          />
        </TextField>
      )}
      {!requiresOtp && (
        <TextField
          name="password"
          type="password"
          isRequired
          isDisabled={isLoading}
        >
          <Label>{t("auth.login.passwordLabel")}</Label>
          <Input
            placeholder={t("auth.login.passwordPlaceholder")}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
          />
        </TextField>
      )}
      {requiresOtp ? (
        <>
          <TextField name="otp" isRequired isDisabled={isLoading}>
            <Label>{t("auth.login.otpLabel")}</Label>
            <Input
              inputMode="numeric"
              autoComplete="one-time-code"
              placeholder={t("auth.login.otpPlaceholder")}
              value={otpCode}
              onChange={(e) => setOtpCode(e.target.value)}
            />
          </TextField>
          <Button type="submit" isDisabled={!isHydrated || isLoading} isPending={isLoading} className="w-full">
            {t("auth.login.otpVerifyAction")}
          </Button>
        </>
      ) : (
        <Button type="submit" isDisabled={!isHydrated || isLoading} isPending={isLoading} className="w-full">
          {t("auth.login.signInAction")}
        </Button>
      )}
      {!requiresOtp && forgotPasswordHref && (
        <a className="self-end text-sm text-primary underline-offset-4 hover:underline" href={forgotPasswordHref}>
          {t("auth.recovery.forgotPasswordAction")}
        </a>
      )}
    </Form>
  );
}
