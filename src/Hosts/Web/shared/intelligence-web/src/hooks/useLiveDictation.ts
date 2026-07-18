"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  useIntelligenceGetLiveTranscriptionCapabilities,
} from "../api/generated/intelligence/intelligence";
import type { RealtimeTranscriptionCapabilities } from "../api/generated/intelligence/model";
import {
  buildLiveTranscriptionSocketUrl,
  getDictationBrowserFailureCode,
  LiveTranscriptionClient,
  selectSupportedLiveTranscriptionMimeType,
} from "../api/intelligence/liveTranscriptionClient";
import type {
  LiveTranscriptionClientConfiguration,
  LiveDictationFailureCode,
  LiveDictationLifecycle,
} from "../api/intelligence/liveTranscriptionClient";

export interface UseLiveDictationOptions {
  enabled: boolean;
  onSegment: (text: string) => void;
  preferredLanguage: string;
  sessionKey: string;
}

export interface LiveDictationController {
  available: boolean;
  capabilitiesLoading: boolean;
  failureCode: LiveDictationFailureCode | null;
  isActive: boolean;
  language: string;
  languages: readonly string[];
  setLanguage: (language: string) => void;
  start: (language?: string) => Promise<void>;
  status: LiveDictationLifecycle | "unavailable";
  stop: () => Promise<void>;
}

export function appendConfirmedTranscript(current: string, segment: string): string {
  const confirmed = segment.trim();
  if (!confirmed) return current;
  if (!current) return confirmed;
  return /\s$/u.test(current) ? `${current}${confirmed}` : `${current} ${confirmed}`;
}

export function selectDictationLanguage(
  languages: readonly string[],
  language: string,
  preferredLanguage: string,
  previousPreferredLanguage: string,
): string {
  if (languages.length === 0) return language;
  if (
    previousPreferredLanguage !== preferredLanguage
    && languages.includes(preferredLanguage)
  ) {
    return preferredLanguage;
  }
  if (languages.includes(language)) return language;
  return languages.includes(preferredLanguage) ? preferredLanguage : languages[0];
}

export function useLiveDictation({
  enabled,
  onSegment,
  preferredLanguage,
  sessionKey,
}: UseLiveDictationOptions): LiveDictationController {
  const capabilitiesQuery = useIntelligenceGetLiveTranscriptionCapabilities({
    query: {
      enabled,
      refetchOnWindowFocus: false,
      retry: false,
      staleTime: 60_000,
    },
  });
  const [language, setLanguage] = useState(preferredLanguage);
  const [lifecycle, setLifecycle] = useState<LiveDictationLifecycle>("idle");
  const [failureCode, setFailureCode] = useState<LiveDictationFailureCode | null>(null);
  const sessionRef = useRef<LiveTranscriptionClient | null>(null);
  const onSegmentRef = useRef(onSegment);
  const previousPreferredLanguageRef = useRef(preferredLanguage);
  onSegmentRef.current = onSegment;

  const configuration = useMemo(
    () => toClientConfiguration(capabilitiesQuery.data, language),
    [capabilitiesQuery.data, language],
  );
  const browserFailure = getDictationBrowserFailureCode();
  const unavailableCode: LiveDictationFailureCode | null = !enabled
    ? "CapabilityUnavailable"
    : capabilitiesQuery.isError || capabilitiesQuery.data?.available === false
      ? "CapabilityUnavailable"
      : browserFailure ?? (capabilitiesQuery.data && !configuration ? "CodecUnavailable" : null);
  const available = Boolean(
    enabled
    && capabilitiesQuery.data?.available
    && configuration
    && !unavailableCode,
  );
  const isActive = lifecycle === "connecting"
    || lifecycle === "recording"
    || lifecycle === "completing";

  useEffect(() => {
    const languages = capabilitiesQuery.data?.languages ?? [];
    if (languages.length === 0) return;

    const nextLanguage = selectDictationLanguage(
      languages,
      language,
      preferredLanguage,
      previousPreferredLanguageRef.current,
    );
    previousPreferredLanguageRef.current = preferredLanguage;
    if (nextLanguage !== language) setLanguage(nextLanguage);
  }, [capabilitiesQuery.data?.languages, language, preferredLanguage]);

  useEffect(() => {
    sessionRef.current?.dispose();
    sessionRef.current = null;
    setLifecycle("idle");
    setFailureCode(null);
  }, [enabled, sessionKey]);

  useEffect(() => () => {
    sessionRef.current?.dispose();
    sessionRef.current = null;
  }, []);

  const start = useCallback(async (selectedLanguage = language) => {
    const selectedConfiguration = selectedLanguage === language
      ? configuration
      : toClientConfiguration(capabilitiesQuery.data, selectedLanguage);
    if (!available || !selectedConfiguration || isActive) {
      if (!isActive) {
        setFailureCode(unavailableCode ?? "CapabilityUnavailable");
        setLifecycle("failed");
      }
      return;
    }

    if (selectedLanguage !== language) setLanguage(selectedLanguage);
    sessionRef.current?.dispose();
    setFailureCode(null);
    let session: LiveTranscriptionClient;
    session = new LiveTranscriptionClient(selectedConfiguration, {
      onFailure: (code) => {
        if (sessionRef.current === session) setFailureCode(code);
      },
      onSegment: (text) => {
        if (sessionRef.current === session) onSegmentRef.current(text);
      },
      onStateChange: (state) => {
        if (sessionRef.current === session) setLifecycle(state);
      },
    });
    sessionRef.current = session;
    try {
      await session.start();
    } catch {
      // The session callback already exposed a stable localized failure code.
    }
  }, [available, capabilitiesQuery.data, configuration, isActive, language, unavailableCode]);

  const stop = useCallback(async () => {
    await sessionRef.current?.stop();
  }, []);

  return {
    available,
    capabilitiesLoading: enabled && capabilitiesQuery.isLoading,
    failureCode: failureCode ?? (!available && !capabilitiesQuery.isLoading ? unavailableCode : null),
    isActive,
    language,
    languages: capabilitiesQuery.data?.languages ?? [],
    setLanguage,
    start,
    status: lifecycle === "idle" && !available ? "unavailable" : lifecycle,
    stop,
  };
}

function toClientConfiguration(
  capabilities: RealtimeTranscriptionCapabilities | undefined,
  language: string,
): LiveTranscriptionClientConfiguration | null {
  if (!capabilities?.available || !capabilities.languages.includes(language)) return null;
  const mimeType = selectSupportedLiveTranscriptionMimeType(capabilities.mimeTypes);
  const maxFrameBytes = Number(capabilities.limits.maxFrameBytes);
  const maxTotalBytes = Number(capabilities.limits.maxTotalBytes);
  const idleTimeoutSeconds = Number(capabilities.limits.idleTimeoutSeconds);
  if (
    !mimeType
    || !Number.isSafeInteger(maxFrameBytes)
    || maxFrameBytes <= 0
    || !Number.isSafeInteger(maxTotalBytes)
    || maxTotalBytes < maxFrameBytes
    || !Number.isSafeInteger(idleTimeoutSeconds)
    || idleTimeoutSeconds <= 0
  ) {
    return null;
  }

  try {
    return {
      language,
      limits: { idleTimeoutSeconds, maxFrameBytes, maxTotalBytes },
      mimeType,
      socketUrl: buildLiveTranscriptionSocketUrl(),
    };
  } catch {
    return null;
  }
}
