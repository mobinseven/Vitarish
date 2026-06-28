import type { AuthenticationChallengePurposeDto } from "./authenticationChallengePurposeDto";

export interface AuthenticationChallengeDto {
  challengeId: string;
  purpose: AuthenticationChallengePurposeDto;
  expiresAt: string;
  remainingAttempts: number;
  optionsJson?: string | null;
}
