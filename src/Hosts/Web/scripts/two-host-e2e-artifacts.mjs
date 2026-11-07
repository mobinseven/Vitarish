export const twoHostE2eApiOriginEnvironmentKey = "VITARISH_E2E_API_ORIGIN";
export const twoHostE2eArtifactDirectoryEnvironmentKey = "VITARISH_E2E_ARTIFACT_DIRECTORY";
export const twoHostE2eAuditEvidenceDirectoryEnvironmentKey = "VITARISH_E2E_AUDIT_EVIDENCE_DIRECTORY";
export const twoHostE2eGrepEnvironmentKey = "VITARISH_E2E_GREP";
export const twoHostE2eNextDistDirectoryEnvironmentKey = "NEXT_DIST_DIR";
export const twoHostE2eProofScreenshotEnvironmentKey = "VITARISH_E2E_PROOF_SCREENSHOT";
export const twoHostE2eSecurityPolicyScreenshotEnvironmentKey = "VITARISH_E2E_SECURITY_POLICY_PROOF_SCREENSHOT";
export const twoHostE2eProofStorageStateEnvironmentKey = "VITARISH_E2E_PROOF_STORAGE_STATE";
export const twoHostE2eLocaleEnvironmentKey = "VITARISH_E2E_LOCALE";
export const twoHostE2eOwnerUsernameEnvironmentKey = "VITARISH_E2E_USERNAME";
export const twoHostE2ePasswordEnvironmentKey = "VITARISH_E2E_PASSWORD";
export const twoHostE2eAccountDeletionClientIp = "198.51.100.241";
export const twoHostE2eForwardedForHeaderName = "X-Forwarded-For";
export const twoHostE2eClinicOrigin = "http://localhost:3005";
export const twoHostE2eLandingOrigin = "http://landing.localhost:3005";
export const twoHostE2eTenantBClinicOrigin = "http://tenant-b.localhost:3005";
export const twoHostE2eTenantBOwnerUsernameEnvironmentKey = "VITARISH_E2E_TENANT_B_OWNER_USERNAME";
export const twoHostE2eTenantBOwnerUsername = "e2e-tenant-b-owner";
export const twoHostE2ePlatformAdminClientIp = "198.51.100.21";
export const twoHostE2ePasswordlessPhoneClientIp = "198.51.100.248";
export const twoHostE2ePlatformAdminOrigin = "http://admin.localhost:3006";
export const twoHostE2ePatientsOrigin = "http://patients.localhost:3007";
export const twoHostE2eProjectManagementOrigin = "http://pm.localhost:3008";
export const twoHostE2eKnowledgeOrigin = "http://knowledge.localhost:3009";
/**
 * The shared sign-in host: the E2E host binds auth.localhost to the sign-in tenant
 * (src/Hosts/E2e/Program.cs), and the runner sends every other host's sign-in pages there.
 */
export const twoHostE2eSignInOrigin = "http://auth.localhost:3005";
/** Where the E2E host writes every SMS it would have sent, one JSON line each. */
export const twoHostE2eSmsCapturePathEnvironmentKey = "VITARISH_E2E_SMS_CAPTURE_PATH";

// Maps the Files-owned FileTransferAuditKeyOptions.CurrentKey setting into the disposable host.
export const twoHostE2eFileTransferAuditKeyEnvironmentKey = "Vitarish__Files__TransferAudit__CurrentKey";

// Environment serialization of the Parties-owned BrowserApplicationHostOptions keys.
export const twoHostE2ePatientsHostConfigurationKey = "Vitarish__PatientsUrl";
export const twoHostE2eProjectManagementHostConfigurationKey = "Vitarish__ProjectManagementUrl";
export const twoHostE2eKnowledgeHostConfigurationKey = "Vitarish__KnowledgeQaUrl";
