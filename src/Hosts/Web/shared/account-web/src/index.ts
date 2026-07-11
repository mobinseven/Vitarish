// Hooks
export { useAccountTheme } from "./hooks/useAccountTheme";
export { useAccountNameUpdate } from "./hooks/useAccountNameUpdate";
export { useAccountEmailChange } from "./hooks/useAccountEmailChange";
export { useAccountPhoneChange } from "./hooks/useAccountPhoneChange";
export { useAccountPasswordChange } from "./hooks/useAccountPasswordChange";
export { useAccountCulture } from "./hooks/useAccountCulture";
export { useAccountPasskeys } from "./hooks/useAccountPasskeys";
export { accountKeys } from "./hooks/accountKeys";

// Theme
export { PaletteSwitch } from "./components/theme/PaletteSwitch";
export {
  accountThemeOptions,
  decodeAccountThemePreference,
  encodeAccountThemePreference,
  isAccountThemeValue,
  setVibrantPaletteAttribute,
  resolveThemeMode,
  setPaletteAttribute,
  vibrantPaletteStorageKey,
  paletteStorageKey,
} from "./components/theme/themeOptions";
export type {
  AccountThemeValue,
  AccountThemePreference,
  AccountThemeOption,
  ThemeMode,
} from "./components/theme/themeOptions";

// Passkey errors
export { localizePasskeyRegistrationError } from "./passkeyRegistrationErrors";

// Passkey registration utilities
export { parsePasskeyRegistrationOptions, PasskeyRegistrationOptionsError } from "./hooks/passkeyRegistrationOptions";
export { PasskeyRegistrationStageError } from "./hooks/passkeyRegistrationStageError";
export type { PasskeyRegistrationStage } from "./hooks/passkeyRegistrationStageError";

// WebAuthn utilities
export {
  startRegistration,
  startPasskeyAuthentication,
  WebAuthnUnsupportedError,
  WebAuthnOptionConversionError,
  WebAuthnRegistrationError,
} from "./auth/webauthn";
export type { PublicKeyCredentialRequestOptionsJSON } from "./auth/webauthn";

// Passkey registration flow
export { registerPasskey } from "./auth/passkeyRegistration";
