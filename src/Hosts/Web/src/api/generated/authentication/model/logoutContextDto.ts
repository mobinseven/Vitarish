export interface LogoutContextDto {
  logoutId?: string | null;
  clientName?: string | null;
  postLogoutRedirectUri?: string | null;
  showLogoutPrompt: boolean;
}
