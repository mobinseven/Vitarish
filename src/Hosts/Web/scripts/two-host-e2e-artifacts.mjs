export const twoHostE2eApiOriginEnvironmentKey = "VITARISH_E2E_API_ORIGIN";
export const twoHostE2eArtifactDirectoryEnvironmentKey = "VITARISH_E2E_ARTIFACT_DIRECTORY";
export const twoHostE2eAuditEvidenceDirectoryEnvironmentKey = "VITARISH_E2E_AUDIT_EVIDENCE_DIRECTORY";
export const twoHostE2eGrepEnvironmentKey = "VITARISH_E2E_GREP";
export const twoHostE2eNextDistDirectoryEnvironmentKey = "NEXT_DIST_DIR";
export const twoHostE2eProofScreenshotEnvironmentKey = "VITARISH_E2E_PROOF_SCREENSHOT";
export const twoHostE2eSecurityPolicyScreenshotEnvironmentKey = "VITARISH_E2E_SECURITY_POLICY_PROOF_SCREENSHOT";
export const twoHostE2eProofStorageStateEnvironmentKey = "VITARISH_E2E_PROOF_STORAGE_STATE";
export const twoHostE2eLocaleEnvironmentKey = "VITARISH_E2E_LOCALE";
export const twoHostE2eClinicOrigin = "http://localhost:3005";
export const twoHostE2ePlatformAdminOrigin = "http://localhost:3006";
/**
 * The shared sign-in host: the E2E host binds auth.localhost to the sign-in tenant
 * (src/Hosts/E2e/Program.cs), and the runner sends every other host's sign-in pages there.
 */
export const twoHostE2eSignInOrigin = "http://auth.localhost:3005";
/** Where the E2E host writes every SMS it would have sent, one JSON line each. */
export const twoHostE2eSmsCapturePathEnvironmentKey = "VITARISH_E2E_SMS_CAPTURE_PATH";

// Maps the Files-owned FileTransferAuditKeyOptions.CurrentKey setting into the disposable host.
export const twoHostE2eFileTransferAuditKeyEnvironmentKey = "Vitarish__Files__TransferAudit__CurrentKey";
