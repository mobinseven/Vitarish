"use client";

import { useCallback, useState } from "react";
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
 * The shared sign-in host's workspace choice. A handoff starts only after an explicit choice.
 */
export function useClinicSelection(handoffPath: string): ClinicSelection {
  const query = usePartiesGetSignInClinics();
  const clinics = query.data?.clinics ?? [];
  const [opening, setOpening] = useState<ClinicSelectionTarget | null>(null);
  const [openFailed, setOpenFailed] = useState(false);
  const [newClinicUnavailable, setNewClinicUnavailable] = useState(false);

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
