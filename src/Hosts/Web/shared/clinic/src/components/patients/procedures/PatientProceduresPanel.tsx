"use client";

import { useState } from "react";
import {
  Alert,
  Button,
  Chip,
  Disclosure,
  Spinner,
  Tooltip,
} from "@heroui/react";
import { EmptyState } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import { usePatientProcedures } from "../../../hooks/patients/procedures/usePatientProcedures";
import { usePatientProcedureTriage } from "../../../hooks/patients/procedures/usePatientProcedureTriage";
import { formatDate, useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { PatientProcedureTemplateSelector } from "./PatientProcedureTemplateSelector";
import { PatientProcedureFocusedLane } from "./PatientProcedureFocusedLane";
import { PatientProcedureProgress } from "./PatientProcedureProgress";

interface PatientProceduresPanelProps {
  patientId: string;
  onNavigate?: (intent: NavigationIntent) => void;
  excludedProcedureName?: string;
}

export function PatientProceduresPanel({
  patientId,
  onNavigate,
  excludedProcedureName,
}: PatientProceduresPanelProps) {
  const { t, locale } = useLocalizedText();
  const {
    data: procedures,
    isLoading,
    error,
  } = usePatientProcedures(patientId);
  const { data: triage } = usePatientProcedureTriage(patientId);
  const [showTemplateSelector, setShowTemplateSelector] = useState(false);
  const [focusedProcedureId, setFocusedProcedureId] = useState<string>();
  const visibleProcedures = procedures?.filter(
    (procedure) => procedure.name !== excludedProcedureName,
  );
  const effectiveFocusedProcedureId = visibleProcedures?.some(
    ({ id }) => id === focusedProcedureId,
  )
    ? focusedProcedureId
    : visibleProcedures?.[0]?.id;

  return (
    <>
      <div className="mb-5 flex flex-wrap items-start justify-between gap-4">
        <Tooltip>
          <Button
            size="sm"
            variant="primary"
            onPress={() => setShowTemplateSelector(true)}
          >
            <FaIcon icon="plus" aria-hidden="true" />
            {t("clinic.procedures.new")}
          </Button>
          <Tooltip.Content>{t("clinic.procedures.new")}</Tooltip.Content>
        </Tooltip>
      </div>

      {isLoading ? (
        <div className="flex min-h-40 items-center justify-center">
          <Spinner
            aria-label={t("common.states.loading")}
            className="self-center"
          />
        </div>
      ) : error ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("clinic.procedures.failedToLoad")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : visibleProcedures?.length ? (
        <div className="overflow-hidden rounded-2xl border border-separator bg-surface">
          {visibleProcedures.map((procedure, index) => {
            const isFocused = procedure.id === effectiveFocusedProcedureId;
            const lastActivity = procedure.lastActivity
              ? formatDate(procedure.lastActivity, locale, {
                  dateStyle: "medium",
                })
              : t("clinic.procedures.progressAtGlance.noActivity");

            return (
              <Disclosure
                key={procedure.id}
                isExpanded={isFocused}
                onExpandedChange={(expanded) =>
                  setFocusedProcedureId(expanded ? procedure.id : undefined)
                }
                className={index > 0 ? "border-t border-separator" : undefined}
              >
                <Disclosure.Heading>
                  <Button
                    slot="trigger"
                    variant="ghost"
                    className="h-auto w-full justify-start rounded-none px-4 py-4 text-start hover:bg-default/20 focus-visible:bg-default/30 sm:px-5"
                  >
                    <div className="grid w-full items-center gap-4 md:grid-cols-[minmax(0,1fr)_minmax(13rem,0.7fr)_auto]">
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="truncate font-semibold">
                            {procedure.name}
                          </span>
                          {procedure.newItems > 0 ? (
                            <Chip size="sm" color="accent" variant="soft">
                              {t(
                                "clinic.procedures.progressAtGlance.newEvidence",
                                { count: procedure.newItems },
                              )}
                            </Chip>
                          ) : null}
                        </div>
                        {procedure.description ? (
                          <span className="mt-1 block line-clamp-2 text-sm text-muted">
                            {procedure.description}
                          </span>
                        ) : null}
                        <span className="mt-2 block text-xs text-muted">
                          {t(
                            "clinic.procedures.progressAtGlance.lastActivity",
                            {
                              value: lastActivity,
                            },
                          )}
                        </span>
                      </div>
                      <PatientProcedureProgress
                        eventCount={procedure.eventCount}
                        committedEventCount={procedure.committedEventCount}
                      />
                      <Disclosure.Indicator className="text-muted" />
                    </div>
                  </Button>
                </Disclosure.Heading>
                <Disclosure.Content>
                  <Disclosure.Body>
                    <PatientProcedureFocusedLane
                      patientId={patientId}
                      procedure={procedure}
                      onNavigate={onNavigate}
                    />
                  </Disclosure.Body>
                </Disclosure.Content>
              </Disclosure>
            );
          })}
        </div>
      ) : (
        <EmptyState size="sm">
          <EmptyState.Header>
            <EmptyState.Media variant="icon">
              <FaIcon icon="list" />
            </EmptyState.Media>
            <EmptyState.Title>{t("clinic.procedures.empty")}</EmptyState.Title>
          </EmptyState.Header>
        </EmptyState>
      )}

      {showTemplateSelector ? (
        <PatientProcedureTemplateSelector
          patientId={patientId}
          onClose={() => setShowTemplateSelector(false)}
        />
      ) : null}
    </>
  );
}
