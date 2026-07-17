"use client";

import { useLocalizedText } from "@vitarish/web-localization";
import type { LiveDictationFailureCode } from "../api/intelligence/liveTranscriptionClient";
import type { LiveDictationController } from "../hooks/useLiveDictation";

export interface LiveDictationStatusProps {
  controller: LiveDictationController;
  className?: string;
}

export function LiveDictationStatus({ controller, className }: LiveDictationStatusProps) {
  const { t } = useLocalizedText();
  const text = controller.capabilitiesLoading
    ? t("intelligence.dictation.status.checking")
    : controller.status === "connecting"
      ? t("intelligence.dictation.status.connecting")
      : controller.status === "recording"
        ? t("intelligence.dictation.status.recording")
        : controller.status === "completing"
          ? t("intelligence.dictation.status.completing")
          : controller.status === "failed" || controller.status === "unavailable"
            ? t(failureMessageKey(controller.failureCode))
            : null;

  if (!text) return null;
  const isAlert = controller.status === "failed";
  return (
    <p
      className={`${className ?? ""} ${isAlert ? "text-danger" : "text-muted"}`.trim()}
      role={isAlert ? "alert" : "status"}
    >
      {text}
    </p>
  );
}

function failureMessageKey(code: LiveDictationFailureCode | null): string {
  switch (code) {
    case "InsecureContext": return "intelligence.dictation.failures.insecureContext";
    case "BrowserUnsupported": return "intelligence.dictation.failures.browserUnsupported";
    case "MicrophoneUnavailable": return "intelligence.dictation.failures.microphoneUnavailable";
    case "PermissionDenied": return "intelligence.dictation.failures.permissionDenied";
    case "CodecUnavailable":
    case "UnsupportedMediaType": return "intelligence.dictation.failures.codecUnavailable";
    case "NetworkUnavailable": return "intelligence.dictation.failures.networkUnavailable";
    case "ProviderTimeout": return "intelligence.dictation.failures.providerTimeout";
    case "InvalidAudio": return "intelligence.dictation.failures.invalidAudio";
    case "FrameTooLarge":
    case "PayloadTooLarge":
    case "DurationLimitExceeded":
    case "IdleTimeout": return "intelligence.dictation.failures.limitReached";
    case "Cancellation": return "intelligence.dictation.failures.canceled";
    case "CapabilityUnavailable":
    case "ProviderUnavailable": return "intelligence.dictation.failures.unavailable";
    default: return "intelligence.dictation.failures.failed";
  }
}
