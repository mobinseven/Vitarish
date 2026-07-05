import { useState, useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useUsersGetProfile,
  useUsersUpdateProfile,
} from "@/api/generated/users/users";
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
  const [initialized, setInitialized] = useState(false);
  const [success, setSuccess] = useState(false);

  if (profile && !initialized) {
    setFirstName(profile.firstName ?? "");
    setLastName(profile.lastName ?? "");
    setInitialized(true);
  }

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
