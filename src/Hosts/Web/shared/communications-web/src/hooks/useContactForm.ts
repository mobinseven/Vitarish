"use client";

import { useCallback, useEffect, useRef, useState } from "react";

export interface ContactFormValues {
  name: string;
  email: string;
  message: string;
  context?: string;
  goal?: string;
  need?: string;
}

export type ContactFormSubmit = (
  values: ContactFormValues,
  options: { signal: AbortSignal },
) => Promise<void>;

export type ContactFormStatus = "idle" | "submitting" | "succeeded" | "failed";

export interface UseContactFormOptions {
  onSubmit: ContactFormSubmit;
  context?: string;
  goal?: string;
  need?: string;
}

export interface ContactFormController {
  values: ContactFormValues;
  setName: (value: string) => void;
  setEmail: (value: string) => void;
  setMessage: (value: string) => void;
  setGoal: (value: string) => void;
  setNeed: (value: string) => void;
  status: ContactFormStatus;
  submit: () => Promise<void>;
  reset: () => void;
}

const emptyValues: ContactFormValues = {
  name: "",
  email: "",
  message: "",
};

export function useContactForm({
  onSubmit,
  context,
  goal,
  need,
}: UseContactFormOptions): ContactFormController {
  const [values, setValues] = useState<ContactFormValues>(() => ({
    ...emptyValues,
    ...(context === undefined ? {} : { context }),
    ...(goal === undefined ? {} : { goal }),
    ...(need === undefined ? {} : { need }),
  }));
  const [status, setStatus] = useState<ContactFormStatus>("idle");
  const abortControllerRef = useRef<AbortController | null>(null);
  const requestIdRef = useRef(0);
  const mountedRef = useRef(true);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
      requestIdRef.current += 1;
      abortControllerRef.current?.abort();
    };
  }, []);

  useEffect(() => {
    setValues((current) => ({
      ...current,
      ...(context === undefined ? { context: undefined } : { context }),
    }));
  }, [context]);

  const updateValue = useCallback(
    (key: "name" | "email" | "message" | "goal" | "need", value: string) => {
      setValues((current) => ({ ...current, [key]: value }));
      setStatus((current) =>
        current === "failed" || current === "succeeded" ? "idle" : current,
      );
    },
    [],
  );

  const reset = useCallback(() => {
    requestIdRef.current += 1;
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
    setValues({
      ...emptyValues,
      ...(context === undefined ? {} : { context }),
      ...(goal === undefined ? {} : { goal }),
      ...(need === undefined ? {} : { need }),
    });
    setStatus("idle");
  }, [context, goal, need]);

  const submit = useCallback(async () => {
    if (abortControllerRef.current) {
      return;
    }

    const requestId = requestIdRef.current + 1;
    requestIdRef.current = requestId;
    const abortController = new AbortController();
    abortControllerRef.current = abortController;
    setStatus("submitting");

    try {
      await onSubmit({ ...values }, { signal: abortController.signal });
      if (!mountedRef.current || requestIdRef.current !== requestId) {
        return;
      }
      setValues({
        ...emptyValues,
        ...(context === undefined ? {} : { context }),
        ...(goal === undefined ? {} : { goal }),
        ...(need === undefined ? {} : { need }),
      });
      setStatus("succeeded");
    } catch {
      if (
        !mountedRef.current ||
        requestIdRef.current !== requestId ||
        abortController.signal.aborted
      ) {
        return;
      }
      setStatus("failed");
    } finally {
      if (requestIdRef.current === requestId) {
        abortControllerRef.current = null;
      }
    }
  }, [context, goal, need, onSubmit, values]);

  return {
    values,
    setName: useCallback(
      (value: string) => updateValue("name", value),
      [updateValue],
    ),
    setEmail: useCallback(
      (value: string) => updateValue("email", value),
      [updateValue],
    ),
    setMessage: useCallback(
      (value: string) => updateValue("message", value),
      [updateValue],
    ),
    setGoal: useCallback(
      (value: string) => updateValue("goal", value),
      [updateValue],
    ),
    setNeed: useCallback(
      (value: string) => updateValue("need", value),
      [updateValue],
    ),
    status,
    submit,
    reset,
  };
}
