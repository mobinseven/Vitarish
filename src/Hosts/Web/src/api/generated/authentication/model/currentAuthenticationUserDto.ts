export interface CurrentAuthenticationUserDto {
  userId: string;
  displayName?: string | null;
  givenName?: string | null;
  familyName?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
}
