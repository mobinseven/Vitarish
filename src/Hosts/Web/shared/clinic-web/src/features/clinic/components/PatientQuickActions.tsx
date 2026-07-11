"use client";

import { useCallback, useState } from "react";
import { Button } from "@heroui/react";
import { PatientDeleteDialog } from "./PatientDeleteDialog";
import { useLocalizedText } from "../../../localization";

interface PatientQuickActionsProps {
  patientId: string;
  onNavigate?: (intent: import("../../../navigation").ClinicNavigationIntent) => void;
}

export function PatientQuickActions({ patientId, onNavigate }: PatientQuickActionsProps) {
  const { t } = useLocalizedText();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  const handleEdit = useCallback(() => {
    onNavigate?.({ kind: "patientEdit", patientId: patientId });
  }, [onNavigate, patientId]);

  return (
    <>
      <div className="flex gap-2">
        <Button variant="primary" onPress={handleEdit}>
          {t("clinic.patient.quickActions.edit")}
        </Button>
        <Button
          variant="danger"
          onPress={() => setShowDeleteDialog(true)}
        >
          {t("clinic.patient.quickActions.delete")}
        </Button>
      </div>

      <PatientDeleteDialog
        isOpen={showDeleteDialog}
        onClose={() => setShowDeleteDialog(false)}
        patientId={patientId}
      />
    </>
  );
}
