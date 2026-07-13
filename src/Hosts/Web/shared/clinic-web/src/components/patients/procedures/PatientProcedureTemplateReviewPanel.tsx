"use client";


import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureTemplateReviewPanelProps {
  templateName: string;
}

export function PatientProcedureTemplateReviewPanel({ templateName }: PatientProcedureTemplateReviewPanelProps) {
  const { t } = useLocalizedText();

  return (
    <section className="rounded-medium border border-divider p-4">
      <header className="mb-3">
        <h2>{t("clinic.procedures.templates.title")}</h2>
      </header>
      <div>
        <div className="flex flex-col gap-2 text-sm text-muted">
          <p>{t("clinic.procedures.templates.createFrom")}: {templateName}</p>
          <p>{t("clinic.procedures.templates.preview")}</p>
        </div>
      </div>
    </section>
  );
}
