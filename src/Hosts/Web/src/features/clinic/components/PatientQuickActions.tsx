"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { PatientDeleteDialog } from "./PatientDeleteDialog";

interface PatientQuickActionsProps {
  patientId: string;
}

export function PatientQuickActions({ patientId }: PatientQuickActionsProps) {
  const router = useRouter();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  const handleEdit = useCallback(() => {
    router.push(Routes.Clinic.Patients.Edit(patientId));
  }, [router, patientId]);

  return (
    <>
      <div className="flex gap-2">
        <Button variant="primary" onPress={handleEdit}>
          Edit
        </Button>
        <Button
          variant="danger"
          onPress={() => setShowDeleteDialog(true)}
        >
          Delete
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
