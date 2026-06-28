import type { AuthenticationErrorCode } from "./authenticationErrorCode";

export interface AuthenticationProblemDto {
  code: AuthenticationErrorCode;
  title: string;
  detail: string;
  status: number;
}
