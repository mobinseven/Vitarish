import { useEffect, useState } from "react";
import { getOtpExpiryPresentation } from "@/lib/auth/otpExpiry";

export function useOtpExpiry(expiresAt: string | null | undefined) {
  const [now, setNow] = useState(() => new Date());

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

  return getOtpExpiryPresentation(expiresAt, now);
}
