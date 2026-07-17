"use client";

import { Label } from "@heroui/react";
import { NativeSelect } from "@heroui-pro/react";
import { useLocalizedText } from "@vitarish/web-localization";
import type { LiveDictationController } from "../hooks/useLiveDictation";

export interface LiveDictationLanguageSelectProps {
  controller: LiveDictationController;
}

export function LiveDictationLanguageSelect({ controller }: LiveDictationLanguageSelectProps) {
  const { t } = useLocalizedText();
  if (controller.languages.length === 0) return null;

  const label = t("intelligence.dictation.language");
  return (
    <NativeSelect className="max-w-36">
      <Label className="sr-only">{label}</Label>
      <NativeSelect.Trigger
        aria-label={label}
        className="h-8 min-w-28 text-xs"
        disabled={controller.isActive || !controller.available}
        value={controller.language}
        onChange={(event) => controller.setLanguage(event.target.value)}
      >
        {controller.languages.map((language) => (
          <NativeSelect.Option key={language} value={language}>
            {language === "en-US" || language === "fa-IR"
              ? t(`intelligence.dictation.languages.${language}`)
              : language}
          </NativeSelect.Option>
        ))}
        <NativeSelect.Indicator />
      </NativeSelect.Trigger>
    </NativeSelect>
  );
}
