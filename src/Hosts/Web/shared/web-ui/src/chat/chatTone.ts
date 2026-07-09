import type { ChatTone } from "./types";

export function getChatToneTextClassName(tone: ChatTone): string {
  switch (tone) {
    case "success":
      return "text-success-600";
    case "warning":
      return "text-warning-600";
    case "danger":
      return "text-danger-600";
    default:
      return "text-muted";
  }
}
