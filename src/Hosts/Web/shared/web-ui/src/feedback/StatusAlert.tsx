"use client";

import { Alert } from "@heroui/react";

export interface StatusAlertProps {
  message: string;
  type?: "error" | "success";
  className?: string;
}

export function StatusAlert({
  className = "",
  message,
  type = "error",
}: StatusAlertProps) {
  return (
    <Alert
      status={type === "error" ? "danger" : "success"}
      className={className}
    >
      <Alert.Indicator />
      <Alert.Content>{message}</Alert.Content>
    </Alert>
  );
}
