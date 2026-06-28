"use client";

import { Alert } from "@heroui/react";

interface StatusMessageProps {
  message: string;
  type?: "error" | "success";
}

export function StatusMessage({ message, type = "error" }: StatusMessageProps) {
  return (
    <Alert
      color={type === "error" ? "danger" : "success"}
      variant="flat"
      className="mb-4"
    >
      {message}
    </Alert>
  );
}
