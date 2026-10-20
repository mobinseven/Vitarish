"use client";

import { useCallback, useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useUsersUpdateProfile } from "../api/generated/users/users";
import { useLocalizedText } from "@vitarish/localization";
import { currentJourneyQueryKeyPrefix } from "./useCurrentJourney";

export interface ProfileCompletionValues {
  firstName: string;
  lastName: string;
}

interface ProfileCompletionErrors {
  firstName?: string;
  lastName?: string;
  form?: string;
}

/**
 * Saves the signed-in user's name, then asks Journey again for the next step, so the gate
 * boundary moves the user on without this form deciding where.
 * @param userId The signed-in user; undefined while the session is still loading.
 */
export function useCompleteProfile(userId: string | undefined) {
  const { t } = useLocalizedText();
  const queryClient = useQueryClient();
  const [values, setValues] = useState<ProfileCompletionValues>({ firstName: "", lastName: "" });
  const [isSubmitted, setIsSubmitted] = useState(false);
  const updateProfile = useUsersUpdateProfile({
    mutation: {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: currentJourneyQueryKeyPrefix }),
    },
  });

  const errors = useMemo<ProfileCompletionErrors>(() => {
    const nextErrors: ProfileCompletionErrors = {};
    if (isSubmitted && !values.firstName.trim()) nextErrors.firstName = t("journey.gates.profile.firstNameRequired");
    if (isSubmitted && !values.lastName.trim()) nextErrors.lastName = t("journey.gates.profile.lastNameRequired");
    if (updateProfile.isError) nextErrors.form = t("journey.gates.profile.saveFailed");
    return nextErrors;
  }, [isSubmitted, values, updateProfile.isError, t]);

  const setField = useCallback(
    (field: keyof ProfileCompletionValues, value: string) => {
      setValues((current) => ({ ...current, [field]: value }));
    },
    []
  );

  const submit = useCallback(() => {
    setIsSubmitted(true);
    const firstName = values.firstName.trim();
    const lastName = values.lastName.trim();
    if (!userId || !firstName || !lastName) return;
    updateProfile.mutate({ userId, data: { firstName, lastName } });
  }, [updateProfile, userId, values]);

  return {
    values,
    errors,
    setField,
    isSubmitting: updateProfile.isPending,
    canSubmit: Boolean(userId) && !updateProfile.isPending,
    submit,
  };
}
