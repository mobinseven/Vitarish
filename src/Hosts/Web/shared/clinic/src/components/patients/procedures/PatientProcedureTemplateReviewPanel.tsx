"use client";

import { Surface } from "@heroui/react";
import { useLocalizedText } from "../../../localization/index";
import { HeaderIcon } from "@vitarish/ui";

interface PatientProcedureTemplateReviewPanelProps {
  templateName: string;
}

export function PatientProcedureTemplateReviewPanel({
  templateName,
}: PatientProcedureTemplateReviewPanelProps) {
  const { t } = useLocalizedText();

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="file-lines" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.procedures.templates.title")}</h2>
          </div>
        </div>
      </div>
      <div>
        <div className="flex flex-col gap-2 text-sm text-muted">
          <p>
            {t("clinic.procedures.templates.createFrom")}: {templateName}
          </p>
          <p>{t("clinic.procedures.templates.preview")}</p>
        </div>
      </div>
    </Surface>
  );
}
