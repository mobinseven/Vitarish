"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  partiesIssueSignInClinicHandoff,
  partiesIssueSignInOnboardingHandoff,
  usePartiesGetSignInClinics,
} from "../../api/generated/parties/parties";
import type { SignInClinicDto, SignInHandoffResponse } from "../../api/generated/parties/model";
import { handoffUrl } from "../handoff/hostUrl";

/** What the person is being taken to: one of their clinics, or the onboarding host to create one. */
export type ClinicSelectionTarget = { kind: "clinic"; tenantId: string } | { kind: "newClinic" };

export interface ClinicSelection {
  clinics: SignInClinicDto[];
  isLoading: boolean;
  loadFailed: boolean;
  /** The target being opened; the page has left or is about to leave. */
  opening: ClinicSelectionTarget | null;
  /** True when the last attempt to open a target failed, so the person can try again. */
  openFailed: boolean;
  /** True when this deployment cannot hand off to the onboarding host. */
  newClinicUnavailable: boolean;
  open: (target: ClinicSelectionTarget) => void;
}

/**
 * The shared sign-in host's clinic choice. One clinic opens by itself, and a person with none
 * goes straight on to create one; several are listed for the person to choose.
 */
export function useClinicSelection(handoffPath: string): ClinicSelection {
  const query = usePartiesGetSignInClinics();
  const clinics = query.data?.clinics ?? [];
  const [opening, setOpening] = useState<ClinicSelectionTarget | null>(null);
  const [openFailed, setOpenFailed] = useState(false);
  const [newClinicUnavailable, setNewClinicUnavailable] = useState(false);
  const autoOpened = useRef(false);

  const open = useCallback(
    (target: ClinicSelectionTarget) => {
      setOpening(target);
      setOpenFailed(false);
      const issue: Promise<SignInHandoffResponse> =
        target.kind === "clinic" ? partiesIssueSignInClinicHandoff(target.tenantId) : partiesIssueSignInOnboardingHandoff();
      issue.then(
        (handoff) => window.location.assign(handoffUrl(handoff.hostname, handoffPath, handoff.token)),
        () => {
          setOpening(null);
          if (target.kind === "newClinic") setNewClinicUnavailable(true);
          else setOpenFailed(true);
        },
      );
    },
    [handoffPath],
  );

  useEffect(() => {
    if (autoOpened.current || query.isLoading || !query.data) return;
    autoOpened.current = true;
    if (query.data.clinics.length === 1) open({ kind: "clinic", tenantId: query.data.clinics[0].tenantId });
    else if (query.data.clinics.length === 0) open({ kind: "newClinic" });
  }, [open, query.data, query.isLoading]);

  return {
    clinics,
    isLoading: query.isLoading,
    loadFailed: query.isError,
    opening,
    openFailed,
    newClinicUnavailable,
    open,
  };
}
