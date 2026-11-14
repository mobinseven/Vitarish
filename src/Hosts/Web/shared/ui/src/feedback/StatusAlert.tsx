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
      role={type === "error" ? "alert" : "status"}
      status={type === "error" ? "danger" : "success"}
      className={className}
    >
      <Alert.Indicator><i className={type === "error" ? "fa-light fa-circle-exclamation" : "fa-light fa-circle-check"} aria-hidden="true" /></Alert.Indicator>
      <Alert.Content>{message}</Alert.Content>
    </Alert>
  );
}
