export interface CompletePhoneLoginRequest {
  challengeId: string;
  code: string;
  returnUrl?: string | null;
}
