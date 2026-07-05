"use client";

import type { ReactNode } from "react";
import { Alert } from "@heroui/react";
import type { KnowledgeChatTone } from "./types";

export function KnowledgeChatCallout({
  tone = "default",
  children,
}: {
  tone?: KnowledgeChatTone;
  children: ReactNode;
}) {
  const alertStatus = {
    default: "default",
    success: "success",
    warning: "warning",
    danger: "danger",
  }[tone] as "default" | "success" | "warning" | "danger";

  return (
    <Alert status={alertStatus} className="text-sm">
      <Alert.Indicator />
      <Alert.Content>{children}</Alert.Content>
    </Alert>
  );
}
