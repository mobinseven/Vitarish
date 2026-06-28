import type { CurrentAuthenticationUserDto } from "./currentAuthenticationUserDto";

export interface AuthenticationSessionDto {
  isAuthenticated: boolean;
  user?: CurrentAuthenticationUserDto | null;
}
