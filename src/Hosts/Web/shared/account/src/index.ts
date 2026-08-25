// Hooks
export { useAccountNameUpdate } from "./hooks/useAccountNameUpdate";
export { useAccountEmailChange } from "./hooks/useAccountEmailChange";
export { useAccountPhoneChange } from "./hooks/useAccountPhoneChange";
export { useAccountPasswordChange } from "./hooks/useAccountPasswordChange";
export { useAccountCulture } from "./hooks/useAccountCulture";
export { useAccountPasskeys } from "./hooks/useAccountPasskeys";
export { accountKeys } from "./hooks/accountKeys";

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
