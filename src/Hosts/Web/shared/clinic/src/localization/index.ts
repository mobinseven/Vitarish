export { clinicMessageCatalogs } from "./catalogs";

/** Where each clinic specialty code (ClinicSpecialties) has its label: `${prefix}.${code}`. */
export const clinicSpecialtyMessagePrefix = "clinic.specialties";

export {
  LocalizationProvider,
  useLocalizedText,
  defaultLocale,
  supportedLocales,
  getLocaleDirection,
  isSupportedLocale,
  normalizeLocale,
  toSupportedLocale,
  resolveLocale,
  parseLocaleCookie,
  serializeLocaleCookie,
  setLocaleCookie,
  localeCookieName,
  formatDate,
  formatDateTime,
  formatTimeOfDay,
  formatNumber,
  formatPercent,
  formatCurrency,
  formatList,
  formatRelativeTime,
  formatFileSize,
  getReactAriaLocale,
  getDateCalendar,
  parseIsoDateValue,
  parseIsoDateTimeValue,
  dateValueToIsoDate,
  getApiErrorMessageKey,
  localizeApiError,
} from "@vitarish/localization";
export type {
  LocalizationContextValue,
  SupportedLocale,
  ResolveLocaleInput,
  ResolvedLocale,
  LocaleResolutionSource,
  SupportedDateCalendar,
  CodeBearingError,
  MessageCatalog,
  MessageKey,
  MessageParameters,
  Translate,
} from "@vitarish/localization";
