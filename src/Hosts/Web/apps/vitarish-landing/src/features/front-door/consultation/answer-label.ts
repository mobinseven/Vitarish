"use client";

import { useCallback } from "react";
import { clinicSpecialtyMessagePrefix } from "@vitarish/clinic/localization";
import { useLocalizedText } from "@vitarish/localization";
import type { ConsultationQuestionKey } from "./consultation-state";

/**
 * Labels a consultation answer. Kinds of clinic are the clinic app's specialties and use its labels,
 * so the page names them exactly as sign-up does; the other answers are the landing page's own.
 */
export function useAnswerLabel() {
  const { t } = useLocalizedText();
  return useCallback(
    (question: ConsultationQuestionKey, answer: string) =>
      question === "kind" ? t(`${clinicSpecialtyMessagePrefix}.${answer}`) : t(`landing.consultation.options.${question}.${answer}`),
    [t],
  );
}
