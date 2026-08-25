"use client";

import { useEffect, useState } from "react";
import {
  isStoredSeaAttachmentContent,
  loadStoredSeaAttachmentContent,
} from "@vitarish/sea";

export interface PatientProcedureAttachmentSource {
  error: boolean;
  loading: boolean;
  url?: string;
}

export function usePatientProcedureAttachmentSource(
  content: string,
  resolveStoredContent: boolean,
): PatientProcedureAttachmentSource {
  const isStoredContent = resolveStoredContent && isStoredSeaAttachmentContent(content);
  const [state, setState] = useState<PatientProcedureAttachmentSource>(() => ({
    error: false,
    loading: isStoredContent,
    url: isStoredContent ? undefined : content,
  }));

  useEffect(() => {
    if (!isStoredContent) {
      setState({ error: false, loading: false, url: content });
      return;
    }

    const controller = new AbortController();
    let objectUrl: string | undefined;
    setState({ error: false, loading: true });

    void loadStoredSeaAttachmentContent(content, controller.signal)
      .then((blob) => {
        if (controller.signal.aborted) return;
        objectUrl = URL.createObjectURL(blob);
        setState({ error: false, loading: false, url: objectUrl });
      })
      .catch((error: unknown) => {
        if (
          controller.signal.aborted
          || (error instanceof DOMException && error.name === "AbortError")
        ) return;
        setState({ error: true, loading: false });
      });

    return () => {
      controller.abort();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [content, isStoredContent]);

  return state;
}
