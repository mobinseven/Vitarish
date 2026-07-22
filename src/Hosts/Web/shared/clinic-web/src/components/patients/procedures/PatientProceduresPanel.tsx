"use client";

import { useState } from "react";
import { Alert, Button, Chip, Separator, Spinner, Tooltip } from "@heroui/react";
import { usePatientProcedures } from "../../../hooks/patients/procedures/usePatientProcedures";
import { usePatientProcedureTriage } from "../../../hooks/patients/procedures/usePatientProcedureTriage";
import { PatientProcedureForm } from "./PatientProcedureForm";
import { PatientProcedureProgress } from "./PatientProcedureProgress";
import { useLocalizedText } from "../../../localization/index";
import type { NavigationIntent } from "../../../navigation";
import { EmptyState, ItemCard, ItemCardGroup, PressableFeedback } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";

const ProcedureCard = ItemCard<"button">;

interface PatientProceduresPanelProps {
  patientId: string;
  onNavigate?: (intent: NavigationIntent) => void;
}

export function PatientProceduresPanel({
  patientId,
  onNavigate,
}: PatientProceduresPanelProps) {
  const { t } = useLocalizedText();
  const {
    data: procedures,
    isLoading,
    error,
  } = usePatientProcedures(patientId);
  const { data: triage } = usePatientProcedureTriage(patientId);
  const [showCreateForm, setShowCreateForm] = useState(false);

  return (
    <>
      {isLoading ? (
        <Spinner
          aria-label={t("common.states.loading")}
          className="self-center"
        />
      ) : error ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("clinic.procedures.failedToLoad")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : (
        <ItemCardGroup variant="transparent">
          <ItemCardGroup.Header className="flex items-center justify-between">
            <Tooltip>
              <Button size="sm" variant="primary" onPress={() => setShowCreateForm(true)}>
                <FaIcon icon="plus" aria-hidden="true" />
                {t("clinic.procedures.new")}
              </Button>
              <Tooltip.Content>{t("clinic.procedures.new")}</Tooltip.Content>
            </Tooltip>
          </ItemCardGroup.Header>

          {procedures?.length ? (
            <ItemCardGroup className="overflow-hidden">
              {procedures.flatMap((procedure, index) => [
                ...(index > 0 ? [<Separator key={`sep-${procedure.id}`} />] : []),
                <ProcedureCard
                  key={procedure.id}
                  className="hover:bg-default/20 active:bg-default-hover/50 relative w-full cursor-pointer overflow-hidden transition-colors"
                  render={(props) => (
                    <button
                      type="button"
                      {...props}
                      onClick={() =>
                        onNavigate?.({
                          kind: "patientProcedureDetail",
                          patientId,
                          procedureId: procedure.id,
                        })
                      }
                    />
                  )}
                >
                  <PressableFeedback.Ripple />
                  <ItemCard.Icon>
                    <FaIcon icon="list-check" aria-hidden="true" />
                  </ItemCard.Icon>
                  <ItemCard.Content>
                    <ItemCard.Title>{procedure.name}</ItemCard.Title>
                    {procedure.description ? (
                      <ItemCard.Description>
                        {procedure.description}
                        <br />
                        <PatientProcedureProgress
                          eventCount={procedure.eventCount}
                          committedEventCount={procedure.committedEventCount}
                        />
                      </ItemCard.Description>
                    ) : (
                      <ItemCard.Description>
                        <PatientProcedureProgress
                          eventCount={procedure.eventCount}
                          committedEventCount={procedure.committedEventCount}
                        />
                      </ItemCard.Description>
                    )}
                  </ItemCard.Content>
                  <ItemCard.Action>
                    <div className="flex items-center gap-2">
                      {procedure.newItems > 0 && (
                        <Chip size="sm" color="accent" variant="soft">
                          {procedure.newItems}
                        </Chip>
                      )}
                    </div>
                  </ItemCard.Action>
                </ProcedureCard>,
              ])}
            </ItemCardGroup>
          ) : (
            <EmptyState size="sm">
              <EmptyState.Header>
                <EmptyState.Media variant="icon">
                  <FaIcon icon="list" />
                </EmptyState.Media>
                <EmptyState.Title>
                  {t("clinic.procedures.empty")}
                </EmptyState.Title>
              </EmptyState.Header>
            </EmptyState>
          )}
        </ItemCardGroup>
      )}

      {showCreateForm && (
        <PatientProcedureForm
          patientId={patientId}
          onClose={() => setShowCreateForm(false)}
        />
      )}
    </>
  );
}
