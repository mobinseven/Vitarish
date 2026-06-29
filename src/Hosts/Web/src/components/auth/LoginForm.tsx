"use client";

import { useState, useCallback } from "react";
import { TextField, Label, Input, Button, Form } from "@heroui/react";
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
      <TextField name="username" isRequired isDisabled={isLoading}>
        <Label>Username or Email</Label>
        <Input
          placeholder="Enter your username or email"
          value={userNameOrEmail}
          onChange={(e) => setUserNameOrEmail(e.target.value)}
          autoComplete="username"
        />
      </TextField>
      <TextField name="password" type="password" isRequired isDisabled={isLoading}>
        <Label>Password</Label>
        <Input
          placeholder="Enter your password"
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
        Sign In
      </Button>
    </Form>
  );
}
