import { useState, useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useUsersGetProfile, useUsersUpdateProfile } from "@vitarish/auth-web";
import { accountKeys } from "./accountKeys";

export function useAccountNameUpdate(userId: string) {
  const queryClient = useQueryClient();
  const { data: profile, isLoading: isLoadingProfile } =
    useUsersGetProfile(userId);
  const updateProfileMutation = useUsersUpdateProfile({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountKeys.profile(userId),
        });
      },
    },
  });

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [syncedProfile, setSyncedProfile] = useState<boolean>(false);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (profile && !syncedProfile) {
      setFirstName(profile.firstName ?? "");
      setLastName(profile.lastName ?? "");
      setSyncedProfile(true);
    }
  }, [profile, syncedProfile]);

  const save = useCallback(
    async (first: string, last: string) => {
      setSuccess(false);
      await updateProfileMutation.mutateAsync({
        userId,
        data: { firstName: first, lastName: last },
      });
      setSuccess(true);
    },
    [updateProfileMutation, userId]
  );

  return {
    profile,
    isLoadingProfile,
    firstName,
    setFirstName,
    lastName,
    setLastName,
    success,
    setSuccess,
    isError: updateProfileMutation.isError,
    isPending: updateProfileMutation.isPending,
    save,
  };
}
