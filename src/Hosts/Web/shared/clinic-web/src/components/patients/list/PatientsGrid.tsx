"use client";

import { useEffect, useRef } from "react";
import { Button, Dropdown, ScrollShadow, Spinner, Toolbar } from "@heroui/react";
import { EmptyState } from "@heroui-pro/react";
import { FaIcon, LoadingStateSurface } from "@vitarish/web-ui";
import type { PatientViewDto, PatientSortBy } from "../../../models/patients/patientSearchModels";
import { useLocalizedText } from "../../../localization/index";
import { PatientSearchBar } from "./PatientSearchBar";
import { PatientRow } from "../find-or-create/PatientRow";

interface PatientsGridProps {
  patients: PatientViewDto[];
  totalCount: number;
  sortBy?: PatientSortBy;
  descending: boolean;
  isLoading?: boolean;
  isLoadingMore?: boolean;
  onAddPatient: () => void;
  onSearch: (search: string) => void;
  onSort: (field: PatientSortBy) => void;
  onSelectPatient: (patientId: string) => void;
  onLoadMore?: () => void;
}

export function PatientsGrid({
  patients,
  totalCount,
  sortBy,
  descending,
  isLoading = false,
  isLoadingMore = false,
  onAddPatient,
  onSearch,
  onSort,
  onSelectPatient,
  onLoadMore,
}: PatientsGridProps) {
  const { t } = useLocalizedText();
  const loadMoreSentinelRef = useRef<HTMLDivElement>(null);
  const loadRequestPendingRef = useRef(false);
  const countLabel =
    totalCount === 1
      ? t("clinic.patients.countFound.one", { count: String(totalCount) })
      : t("clinic.patients.countFound.other", { count: String(totalCount) });
  const getSortLabel = (field: PatientSortBy) => {
    if (field === "Name") return t("clinic.patient.fields.firstName");
    if (field === "IdentificationCode") return t("clinic.patient.fields.identificationCode");
    return t("clinic.patient.fields.dateOfBirth");
  };

  useEffect(() => {
    if (!isLoadingMore) {
      loadRequestPendingRef.current = false;
    }
  }, [isLoadingMore, patients.length]);

  useEffect(() => {
    const sentinel = loadMoreSentinelRef.current;
    if (!sentinel || !onLoadMore || isLoadingMore || typeof IntersectionObserver === "undefined") {
      return;
    }

    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting && !loadRequestPendingRef.current) {
        loadRequestPendingRef.current = true;
        onLoadMore();
      }
    }, { root: sentinel.closest("[data-slot='scroll-shadow']") });

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [isLoadingMore, onLoadMore]);

  return (
    <div className="grid h-full min-h-0 grid-rows-[auto_minmax(0,1fr)] gap-4">
      <Toolbar
        aria-label={t("clinic.patients.title")}
        isAttached={true}
        className="sticky top-0 z-10 grid w-full shrink-0 grid-flow-row grid-cols-1 gap-2 md:flex md:flex-wrap md:items-center md:gap-3"
      >
        {/* Search: full width row on mobile, flexible column on desktop */}
        <div className="order-0 w-full min-w-0 md:order-none md:flex-1">
          <PatientSearchBar onSearch={onSearch} />
        </div>

        {/* Mobile: single row holding count + sort + add. Desktop: dissolves via contents so each child follows its own md:order-N */}
        <div className="flex w-full items-center justify-between gap-2 md:contents">
          <span
            aria-label={countLabel}
            className="shrink-0 truncate tabular-nums md:order-3 md:justify-self-start"
          >
            {countLabel}
          </span>

          <div className="flex shrink-0 items-center gap-2 md:contents">
            <Dropdown>
              <Button
                size="sm"
                aria-label={t("clinic.patients.sortBy")} variant="secondary">
                <i aria-hidden="true" className="fa-solid fa-arrow-down-a-z" />
                <span className="hidden md:inline">{t("clinic.patients.sortBy")}</span>
              </Button>
              <Dropdown.Popover>
                <Dropdown.Menu
                  aria-label={t("clinic.patients.sortBy")}
                  onAction={(key) => onSort(key as PatientSortBy)}
                >
                  {(["Name", "IdentificationCode", "DateOfBirth"] as PatientSortBy[]).map((field) => (
                    <Dropdown.Item key={field} id={field} textValue={getSortLabel(field)}>
                      {getSortLabel(field)}
                      {sortBy === field ? <span aria-hidden="true">{descending ? "↓" : "↑"}</span> : null}
                    </Dropdown.Item>
                  ))}
                </Dropdown.Menu>
              </Dropdown.Popover>
            </Dropdown>

            <Button
              aria-label={t("clinic.patients.newPatient")}
              size="sm"
              variant="primary"
              onPress={onAddPatient}
            >
              <FaIcon aria-hidden="true" className="shrink-0 text-base" icon="plus" />
              <span className="hidden md:inline">{t("clinic.patients.newPatient")}</span>
            </Button>
          </div>
        </div>
      </Toolbar>
      {isLoading ? (
        <LoadingStateSurface label={t("clinic.patients.loading")} />
      ) : (
        <ScrollShadow className="h-full min-h-0">
          {patients.length === 0 ? (
            <EmptyState size="sm">
              <EmptyState.Header>
                <EmptyState.Media variant="icon">
                  <FaIcon aria-hidden="true" className="text-2xl" icon="users" />
                </EmptyState.Media>
                <EmptyState.Title>{t("clinic.patients.emptyState")}</EmptyState.Title>
                <EmptyState.Description>{t("clinic.patients.searchEmpty")}</EmptyState.Description>
              </EmptyState.Header>
            </EmptyState>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {patients.map((patient) => (
                <PatientRow
                  key={patient.id}
                  patient={patient}
                  variant="secondary"
                  onSelect={onSelectPatient}
                />
              ))}
              {onLoadMore && <div ref={loadMoreSentinelRef} className="h-px sm:col-span-2 xl:col-span-3" />}
              {isLoadingMore && (
                <div className="flex justify-center py-3 sm:col-span-2 xl:col-span-3">
                  <Spinner aria-label={t("common.states.loading")} size="sm" />
                </div>
              )}
            </div>
          )}
        </ScrollShadow>
      )}
    </div>
  );
}
