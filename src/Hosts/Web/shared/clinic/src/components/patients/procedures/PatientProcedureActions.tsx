"use client";

import { useState, type Key } from "react";
import { Button, Dropdown, Label, Separator } from "@heroui/react";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../../localization/index";
import { PatientProcedureForm } from "./PatientProcedureForm";
import { PatientProcedureDeleteDialog } from "./PatientProcedureDeleteDialog";
import type { PatientProcedureDetail } from "../../../models/patients/procedures/PatientProcedureModels";

interface PatientProcedureActionsProps {
  procedure: PatientProcedureDetail;
  patientId: string;
  onDeleted: () => void;
}

export function PatientProcedureActions({
  procedure,
  patientId,
  onDeleted,
}: PatientProcedureActionsProps) {
  const { t, locale } = useLocalizedText();
  const [showEditForm, setShowEditForm] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const handleAction = (key: Key) => {
    if (key === "edit") setShowEditForm(true);
    if (key === "delete") setShowDeleteDialog(true);
  };

  return (
    <>
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-4">
            <HeaderIcon icon="stethoscope" level="page" />
            <div className="min-w-0">
              <h1 className="text-2xl font-bold">{procedure.name}</h1>
            </div>
          </div>
          {procedure.description && (
            <p className="text-sm text-muted mt-1">{procedure.description}</p>
          )}
          <div className="flex items-center gap-3 mt-2 text-sm text-muted">
            <span>
              {t("clinic.procedures.events.count", {
                count: procedure.eventCount,
              })}
            </span>
            {procedure.committedEventCount > 0 && (
              <span>
                {procedure.committedEventCount}{" "}
                {t("clinic.procedures.committed")}
              </span>
            )}
          </div>
        </div>
        <Dropdown>
          <Dropdown.Trigger
            aria-label={t("clinic.procedures.events.actionMenu.label")}
          >
            <FaIcon
              icon="ellipsis-vertical"
              aria-hidden="true"
              className="text-lg"
            />
          </Dropdown.Trigger>
          <Dropdown.Popover>
            <Dropdown.Menu onAction={handleAction}>
              <Dropdown.Item id="edit" textValue={t("common.actions.edit")}>
                <Label>{t("common.actions.edit")}</Label>
              </Dropdown.Item>
              <Dropdown.Item
                id="delete"
                textValue={t("common.actions.delete")}
                variant="danger"
              >
                <Label>{t("common.actions.delete")}</Label>
              </Dropdown.Item>
            </Dropdown.Menu>
          </Dropdown.Popover>
        </Dropdown>
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
