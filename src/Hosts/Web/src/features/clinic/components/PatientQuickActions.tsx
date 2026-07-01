"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { PatientDeleteDialog } from "./PatientDeleteDialog";
import { useLocalizedText } from "@/localization";

interface PatientQuickActionsProps {
  patientId: string;
}

export function PatientQuickActions({ patientId }: PatientQuickActionsProps) {
  const router = useRouter();
  const { t } = useLocalizedText();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  const handleEdit = useCallback(() => {
    router.push(Routes.Clinic.Patients.Edit(patientId));
  }, [router, patientId]);

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
