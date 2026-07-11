"use client";

import { useState } from "react";
import { Button } from "@heroui/react";
import { useLocalizedText, formatDateTime } from "../../../../../localization";
import { PatientProcedureForm } from "./PatientProcedureForm";
import { PatientProcedureDeleteDialog } from "./PatientProcedureDeleteDialog";
import type { PatientProcedureDetail } from "../models/PatientProcedureModels";

interface PatientProcedureHeaderProps {
  procedure: PatientProcedureDetail;
  patientId: string;
  onDeleted: () => void;
}

export function PatientProcedureHeader({ procedure, patientId, onDeleted }: PatientProcedureHeaderProps) {
  const { t, locale } = useLocalizedText();
  const [showEditForm, setShowEditForm] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  return (
    <>
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold">{procedure.name}</h1>
          {procedure.description && (
            <p className="text-sm text-muted mt-1">{procedure.description}</p>
          )}
          <div className="flex items-center gap-3 mt-2 text-xs text-muted">
            <span>{procedure.eventCount} {t("clinic.procedures.events")}</span>
            {procedure.committedEventCount > 0 && (
              <span>{procedure.committedEventCount} {t("clinic.procedures.committed")}</span>
            )}
            {procedure.lastActivity && (
              <span>{t("clinic.procedures.list.lastActivity")}: {formatDateTime(procedure.lastActivity, locale)}</span>
            )}
          </div>
        </div>
        <div className="flex gap-2">
          <Button size="sm" variant="secondary" onPress={() => setShowEditForm(true)}>
            {t("common.actions.edit")}
          </Button>
          <Button size="sm" variant="danger" onPress={() => setShowDeleteDialog(true)}>
            {t("common.actions.delete")}
          </Button>
        </div>
      </div>

      {showEditForm && (
        <PatientProcedureForm
          patientId={patientId}
          procedureId={procedure.id}
          initialName={procedure.name}
          initialDescription={procedure.description}
          onClose={() => setShowEditForm(false)}
        />
      )}

      {showDeleteDialog && (
        <PatientProcedureDeleteDialog
          procedureId={procedure.id}
          procedureName={procedure.name}
          eventCount={procedure.eventCount}
          onClose={() => setShowDeleteDialog(false)}
          onDeleted={onDeleted}
        />
      )}
    </>
  );
}
