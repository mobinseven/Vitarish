"use client";

import { useState } from "react";
import { Button, ButtonGroup, Spinner, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/web-localization";
import type { LiveDictationFailureCode } from "../api/intelligence/liveTranscriptionClient";
import type { LiveDictationController } from "../hooks/useLiveDictation";

export interface LiveDictationProps {
  controller: LiveDictationController;
}

export function LiveDictation({ controller }: LiveDictationProps) {
  const { t } = useLocalizedText();
  const [isChoosingLanguage, setIsChoosingLanguage] = useState(false);
  const isBusy =
    controller.capabilitiesLoading ||
    controller.status === "connecting" ||
    controller.status === "completing";
  const isRecording = controller.status === "recording";
  const canChooseLanguage =
    controller.available &&
    !controller.isActive &&
    controller.languages.length > 0;
  const hasFailure =
    controller.status === "failed" || controller.status === "unavailable";
  const actionLabel = controller.capabilitiesLoading
    ? t("intelligence.dictation.status.checking")
    : isRecording
      ? t("intelligence.dictation.actions.stop")
      : controller.status === "connecting"
        ? t("intelligence.dictation.actions.connecting")
        : controller.status === "completing"
          ? t("intelligence.dictation.actions.completing")
          : controller.status === "unavailable"
            ? t("intelligence.dictation.actions.unavailable")
            : t("intelligence.dictation.actions.start");
  const tooltipText = hasFailure
    ? t(failureMessageKey(controller.failureCode))
    : actionLabel;

  const chooseLanguage = (language: string) => {
    setIsChoosingLanguage(false);
    controller.setLanguage(language);
    void controller.start(language);
  };

  if (isChoosingLanguage && canChooseLanguage) {
    return (
      <ButtonGroup size="sm" variant="secondary">
        {controller.languages.map((language) => (
          <Button key={language} onPress={() => chooseLanguage(language)}>
            {language === "en-US" || language === "fa-IR"
              ? t(`intelligence.dictation.languages.${language}`)
              : language}
          </Button>
        ))}
      </ButtonGroup>
    );
  }

  return (
    <Tooltip delay={500}>
      <Tooltip.Trigger>
        <Button
          aria-label={actionLabel}
          aria-pressed={isRecording}
          isDisabled={!controller.isActive && !canChooseLanguage}
          isIconOnly
          isPending={isBusy}
          size="sm"
          variant={isRecording ? "danger" : "ghost"}
          onPress={() => {
            if (controller.isActive) void controller.stop();
            else if (canChooseLanguage) setIsChoosingLanguage(true);
          }}
        >
          {isBusy ? (
            <Spinner aria-hidden="true" color="current" size="sm" />
          ) : (
            <i
              aria-hidden="true"
              className={`fa-solid ${isRecording ? "fa-stop" : "fa-microphone"}`}
            />
          )}
        </Button>
      </Tooltip.Trigger>
      <Tooltip.Content offset={6} placement="top">
        <Tooltip.Arrow />
        {tooltipText}
      </Tooltip.Content>
    </Tooltip>
  );
}

function failureMessageKey(code: LiveDictationFailureCode | null): string {
  switch (code) {
    case "InsecureContext":
      return "intelligence.dictation.failures.insecureContext";
    case "BrowserUnsupported":
      return "intelligence.dictation.failures.browserUnsupported";
    case "MicrophoneUnavailable":
      return "intelligence.dictation.failures.microphoneUnavailable";
    case "PermissionDenied":
      return "intelligence.dictation.failures.permissionDenied";
    case "CodecUnavailable":
    case "UnsupportedMediaType":
      return "intelligence.dictation.failures.codecUnavailable";
    case "NetworkUnavailable":
      return "intelligence.dictation.failures.networkUnavailable";
    case "ProviderTimeout":
      return "intelligence.dictation.failures.providerTimeout";
    case "InvalidAudio":
      return "intelligence.dictation.failures.invalidAudio";
    case "FrameTooLarge":
    case "PayloadTooLarge":
    case "DurationLimitExceeded":
    case "IdleTimeout":
      return "intelligence.dictation.failures.limitReached";
    case "Cancellation":
      return "intelligence.dictation.failures.canceled";
    case "CapabilityUnavailable":
    case "ProviderUnavailable":
      return "intelligence.dictation.failures.unavailable";
    default:
      return "intelligence.dictation.failures.failed";
  }
}
