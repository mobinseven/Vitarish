"use client";

import { Widget } from "@heroui-pro/react";
import { useLocalizedText } from "@/localization";

interface PatientProcedureTemplateReviewPanelProps {
  templateName: string;
}

export function PatientProcedureTemplateReviewPanel({ templateName }: PatientProcedureTemplateReviewPanelProps) {
  const { t } = useLocalizedText();

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("clinic.procedures.templates.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        <div className="flex flex-col gap-2 text-sm text-muted">
          <p>{t("clinic.procedures.templates.createFrom")}: {templateName}</p>
          <p>{t("clinic.procedures.templates.preview")}</p>
        </div>
      </Widget.Content>
    </Widget>
  );
}
