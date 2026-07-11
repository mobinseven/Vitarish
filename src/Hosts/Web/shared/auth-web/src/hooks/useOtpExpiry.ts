"use client";

import { useEffect, useState } from "react";
import { getOtpExpiryPresentation } from "../auth/otpExpiry";

export function useOtpExpiry(expiresAt: string | null | undefined) {
  const [now, setNow] = useState<Date | null>(null);

  useEffect(() => {
    setNow(new Date());

    if (!expiresAt) {
      return undefined;
    }

    const interval = window.setInterval(() => {
      setNow(new Date());
    }, 1000);

    return () => window.clearInterval(interval);
  }, [expiresAt]);

  if (!now) {
    return { remainingSeconds: null, isExpired: false, label: null };
  }

  return getOtpExpiryPresentation(expiresAt, now);
}
