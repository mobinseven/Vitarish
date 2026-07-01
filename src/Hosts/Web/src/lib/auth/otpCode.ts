const OTP_DIGIT_PATTERN = /[^\d۰-۹٠-٩]/g;
const PERSIAN_DIGIT_PATTERN = /[۰-۹]/g;
const ARABIC_INDIC_DIGIT_PATTERN = /[٠-٩]/g;

export function normalizeOtpCode(input: string, maxLength = 6): string {
  const digitLimit = Math.max(0, maxLength);

  return input
    .replace(OTP_DIGIT_PATTERN, "")
    .replace(PERSIAN_DIGIT_PATTERN, (digit) => String(digit.charCodeAt(0) - 0x06f0))
    .replace(ARABIC_INDIC_DIGIT_PATTERN, (digit) => String(digit.charCodeAt(0) - 0x0660))
    .slice(0, digitLimit);
}
