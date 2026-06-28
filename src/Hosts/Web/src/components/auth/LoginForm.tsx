"use client";

import { useState, useCallback } from "react";
import { Input, Button, Form } from "@heroui/react";
import { usePasswordLogin } from "@/hooks/auth/usePasswordLogin";
import { StatusMessage } from "./StatusMessage";

interface LoginFormProps {
  returnUrl?: string;
}

export function LoginForm({ returnUrl }: LoginFormProps) {
  const { login, isLoading, errorMessage } = usePasswordLogin();
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
      <Input
        label="Username or Email"
        placeholder="Enter your username or email"
        value={userNameOrEmail}
        onValueChange={setUserNameOrEmail}
        isRequired
        autoComplete="username"
        isDisabled={isLoading}
      />
      <Input
        label="Password"
        placeholder="Enter your password"
        type="password"
        value={password}
        onValueChange={setPassword}
        isRequired
        autoComplete="current-password"
        isDisabled={isLoading}
      />
      <Button
        type="submit"
        color="primary"
        isLoading={isLoading}
        className="w-full"
      >
        Sign In
      </Button>
    </Form>
  );
}
