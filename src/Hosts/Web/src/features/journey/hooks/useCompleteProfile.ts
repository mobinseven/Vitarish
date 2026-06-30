import { useCallback, useMemo, useState } from "react";

export interface ProfileCompletionValues {
  firstName: string;
  lastName: string;
}

interface ProfileCompletionErrors {
  firstName?: string;
  lastName?: string;
  form?: string;
}

export function useCompleteProfile() {
  const [values, setValues] = useState<ProfileCompletionValues>({ firstName: "", lastName: "" });
  const [isSubmitting] = useState(false);

  const errors = useMemo<ProfileCompletionErrors>(() => {
    const nextErrors: ProfileCompletionErrors = {};
    if (!values.firstName.trim()) nextErrors.firstName = "First name is required.";
    if (!values.lastName.trim()) nextErrors.lastName = "Last name is required.";
    nextErrors.form = "Profile update is not available yet. This form is ready for the profile API when it is connected.";
    return nextErrors;
  }, [values]);

  const setField = useCallback(
    (field: keyof ProfileCompletionValues, value: string) => {
      setValues((current) => ({ ...current, [field]: value }));
    },
    []
  );

  return {
    values,
    errors,
    setField,
    isSubmitting,
    canSubmit: false,
    submit: undefined,
  };
}
