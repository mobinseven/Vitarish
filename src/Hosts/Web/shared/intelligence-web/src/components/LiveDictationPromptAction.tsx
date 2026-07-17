"use client";

import { PromptInput } from "@heroui-pro/react";
import { useLocalizedText } from "@vitarish/web-localization";
import type { LiveDictationController } from "../hooks/useLiveDictation";

export interface LiveDictationPromptActionProps {
  controller: LiveDictationController;
}

export function LiveDictationPromptAction({ controller }: LiveDictationPromptActionProps) {
  const { t } = useLocalizedText();
  const label = controller.status === "recording"
    ? t("intelligence.dictation.actions.stop")
    : controller.status === "connecting"
      ? t("intelligence.dictation.actions.connecting")
      : controller.status === "completing"
        ? t("intelligence.dictation.actions.completing")
        : controller.status === "unavailable"
          ? t("intelligence.dictation.actions.unavailable")
          : t("intelligence.dictation.actions.start");
  const loading = controller.capabilitiesLoading
    || controller.status === "connecting"
    || controller.status === "completing";

  return (
    <PromptInput.Action
      aria-label={label}
      aria-pressed={controller.status === "recording"}
      className={controller.status === "recording" ? "text-danger" : undefined}
      isDisabled={loading || controller.status === "unavailable"}
      tooltip={label}
      onPress={() => {
        if (controller.status === "recording") void controller.stop();
        else void controller.start();
      }}
    >
      <i
        aria-hidden="true"
        className={`fa-solid size-4 ${
          controller.status === "recording"
            ? "fa-stop"
            : loading
              ? "fa-spinner fa-spin"
              : controller.status === "unavailable"
                ? "fa-microphone-slash"
                : "fa-microphone"
        }`}
      />
    </PromptInput.Action>
  );
}
