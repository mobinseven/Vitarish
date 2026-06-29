"use client";

import { Alert } from "@heroui/react";

interface StatusMessageProps {
  message: string;
  type?: "error" | "success";
}

export function StatusMessage({ message, type = "error" }: StatusMessageProps) {
  return (
    <Alert status={type === "error" ? "danger" : "success"} className="mb-4">
      <Alert.Indicator />
      <Alert.Content>{message}</Alert.Content>
    </Alert>
  );
}
