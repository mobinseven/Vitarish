"use client";

import { useState } from "react";
import { Button, ButtonGroup, Spinner, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
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
  const actionLabel = isRecording
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

  const statusMessage = controller.capabilitiesLoading
    ? t("intelligence.dictation.status.checking")
    : isRecording
      ? t("intelligence.dictation.status.recording")
      : controller.status === "connecting"
        ? t("intelligence.dictation.status.connecting")
        : hasFailure
          ? t(failureMessageKey(controller.failureCode))
          : null;

  return (
    <>
      <ButtonGroup
        aria-hidden={isChoosingLanguage && canChooseLanguage ? "false" : "true"}
        aria-label={t("intelligence.dictation.language")}
        className={isChoosingLanguage && canChooseLanguage ? undefined : "hidden"}
        size="sm"
        variant="secondary"
      >
        {controller.languages.map((language) => (
          <Button key={language} onPress={() => chooseLanguage(language)}>
            {language === "en-US" || language === "fa-IR"
              ? t(`intelligence.dictation.languages.${language}`)
              : language}
          </Button>
        ))}
      </ButtonGroup>
      <Tooltip delay={500}>
        <Tooltip.Trigger>
          <Button
            aria-label={actionLabel}
            aria-pressed={isRecording}
            className={isRecording ? "text-danger animate-pulse" : undefined}
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
            {isBusy ? <Spinner aria-hidden="true" color="current" size="sm" /> : (
              <FaIcon icon={isRecording ? "stop" : "microphone"} aria-hidden="true" />
            )}
          </Button>
        </Tooltip.Trigger>
        <Tooltip.Content offset={6} placement="top">
          <Tooltip.Arrow />
          {tooltipText}
        </Tooltip.Content>
      </Tooltip>
      {statusMessage ? (
        <p role={hasFailure && controller.status === "failed" ? "alert" : "status"} className="sr-only">
          {statusMessage}
        </p>
      ) : null}
    </>
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
