export interface UserProfileResponse {
  userId: string;
  userName: string | null;
  firstName: string | null;
  lastName: string | null;
  email: string | null;
  phoneNumber: string | null;
  culture: string | null;
  theme: string | null;
}
