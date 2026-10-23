"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError } from "@vitarish/api-client";
import {
  partiesCreateOwnTenant,
  usePartiesGetOnboardingCategories,
  usePartiesGetOnboardingSetupDraft,
  usePartiesGetSubdomainAvailability,
} from "../../api/generated/parties/parties";
import type { CreateOwnTenantResponse, TenantSubdomainAvailabilityStatus } from "../../api/generated/parties/model";
import { emptyBusinessDetailsForm, toBusinessDetailsDto, type BusinessDetailsForm } from "./businessDetailsForm";

/** The header the create-clinic endpoint replays by; one key per wizard, so a retry never creates twice. */
const idempotencyKeyHeader = "Idempotency-Key";
const subdomainCheckDelayMilliseconds = 400;
/** Shorter labels are never valid, so they are not sent for checking. */
const subdomainMinimumLength = 3;

export type TenantSetupStep = 0 | 1;

/** Why creation failed, as the wizard explains it; the server decided each one. */
export type TenantSetupFailure = "invalid" | "addressTaken" | "phoneNotVerified" | "limitReached" | "unavailable";

export interface TenantSetupValues {
  name: string;
  subdomain: string;
  category: string | null;
  description: string;
  businessDetails: BusinessDetailsForm;
}

export interface CreatedTenant {
  hostname: string;
  handoffToken: string | null;
}

function failureFor(error: unknown): TenantSetupFailure {
  if (!(error instanceof ApiError)) return "unavailable";
  switch (error.status) {
    case 400:
      return "invalid";
    case 409:
      return "addressTaken";
    case 403:
      return "phoneNotVerified";
    case 429:
      return "limitReached";
    default:
      return "unavailable";
  }
}

function useDebouncedValue(value: string, delayMilliseconds: number): string {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMilliseconds);
    return () => window.clearTimeout(timer);
  }, [value, delayMilliseconds]);
  return debounced;
}

export function useTenantSetup(locale: string) {
  const [step, setStep] = useState<TenantSetupStep>(0);
  const [values, setValues] = useState<TenantSetupValues>(() => ({
    name: "",
    subdomain: "",
    category: null,
    description: "",
    businessDetails: emptyBusinessDetailsForm(locale),
  }));
  const [isCreating, setIsCreating] = useState(false);
  const [failure, setFailure] = useState<TenantSetupFailure | null>(null);
  const [created, setCreated] = useState<CreatedTenant | null>(null);
  const idempotencyKey = useRef<string | null>(null);

  const normalizedSubdomain = values.subdomain.trim().toLowerCase();
  const subdomainToCheck = useDebouncedValue(normalizedSubdomain, subdomainCheckDelayMilliseconds);
  const isChecking = subdomainToCheck !== normalizedSubdomain;
  const availability = usePartiesGetSubdomainAvailability(
    { subdomain: subdomainToCheck },
    { query: { enabled: subdomainToCheck.length >= subdomainMinimumLength, retry: false } },
  );
  const categories = usePartiesGetOnboardingCategories({ query: { retry: false } });
  // What the person said before sign-up (for example in the landing consultation) pre-selects the
  // category once; anything they choose here wins.
  const draft = usePartiesGetOnboardingSetupDraft({ query: { retry: false } });
  const draftCategory = draft.data?.category ?? null;
  const appliedDraft = useRef(false);
  useEffect(() => {
    if (appliedDraft.current || !draftCategory) return;
    appliedDraft.current = true;
    setValues((current) => (current.category === null ? { ...current, category: draftCategory } : current));
  }, [draftCategory]);

  const availabilityStatus: TenantSubdomainAvailabilityStatus | null =
    isChecking || subdomainToCheck.length < subdomainMinimumLength ? null : (availability.data?.status ?? null);
  const hostname = availabilityStatus === "Available" ? (availability.data?.hostname ?? null) : null;
  const canContinue = values.name.trim() !== "" && availabilityStatus === "Available" && !availability.isFetching;

  const setField = useCallback(<TKey extends keyof TenantSetupValues>(key: TKey, value: TenantSetupValues[TKey]) => {
    setValues((current) => ({ ...current, [key]: value }));
    setFailure(null);
  }, []);

  const create = useCallback(async () => {
    idempotencyKey.current ??= crypto.randomUUID();
    setIsCreating(true);
    setFailure(null);
    try {
      const response: CreateOwnTenantResponse = await partiesCreateOwnTenant(
        {
          name: values.name.trim(),
          subdomain: normalizedSubdomain,
          description: values.description.trim() === "" ? null : values.description.trim(),
          category: values.category,
          businessDetails: toBusinessDetailsDto(values.businessDetails),
        },
        { headers: { [idempotencyKeyHeader]: idempotencyKey.current } },
      );
      setCreated({ hostname: response.hostname, handoffToken: response.handoffToken ?? null });
    } catch (error) {
      const nextFailure = failureFor(error);
      setFailure(nextFailure);
      // Keep the key: a refused request created nothing under it, and a lost response must
      // replay rather than create a second clinic. A taken address is fixed on the first step.
      if (nextFailure === "addressTaken") {
        setStep(0);
        void availability.refetch();
      }
    } finally {
      setIsCreating(false);
    }
  }, [availability, normalizedSubdomain, values]);

  return {
    step,
    setStep,
    values,
    setField,
    availabilityStatus,
    isCheckingAvailability: isChecking || availability.isFetching,
    availabilityFailed: availability.isError,
    hostname,
    canContinue,
    categories: categories.data?.categories ?? [],
    categoriesFailed: categories.isError,
    create,
    isCreating,
    failure,
    created,
  };
}
