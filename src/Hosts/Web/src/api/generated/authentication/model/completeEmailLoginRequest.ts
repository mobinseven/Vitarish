export interface CompleteEmailLoginRequest {
  challengeId: string;
  code: string;
  returnUrl?: string | null;
}
