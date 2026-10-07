export interface OtpExpiryPresentation {
  remainingSeconds: number | null;
  isExpired: boolean;
  /** Remaining time as mm:ss; null when there is no expiry or it has passed. */
  remaining: string | null;
}

export function getOtpExpiryPresentation(
  expiresAt: string | null | undefined,
  now: Date = new Date()
): OtpExpiryPresentation {
  if (!expiresAt) {
    return { remainingSeconds: null, isExpired: false, remaining: null };
  }

  const expiryTime = new Date(expiresAt).getTime();

  if (Number.isNaN(expiryTime)) {
    return { remainingSeconds: null, isExpired: false, remaining: null };
  }

  const remainingSeconds = Math.max(
    0,
    Math.ceil((expiryTime - now.getTime()) / 1000)
  );
  const isExpired = remainingSeconds === 0;

  if (isExpired) {
    return {
      remainingSeconds,
      isExpired,
      remaining: null,
    };
  }

  const minutes = Math.floor(remainingSeconds / 60);
  const seconds = remainingSeconds % 60;

  return {
    remainingSeconds,
    isExpired,
    remaining: `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`,
  };
}
