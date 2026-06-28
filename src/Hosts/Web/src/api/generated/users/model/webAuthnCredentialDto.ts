export interface WebAuthnCredentialDto {
  id: string;
  friendlyName: string | null;
  registeredAt: string;
  lastUsedAt: string | null;
  transports: string | null;
}
