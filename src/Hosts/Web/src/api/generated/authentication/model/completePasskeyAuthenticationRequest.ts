export interface CompletePasskeyAuthenticationRequest {
  challengeId: string;
  credentialJson: string;
  returnUrl?: string | null;
}
