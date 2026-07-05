import { useState, useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useUsersGetTheme,
  useUsersSetTheme,
} from "@/api/generated/users/users";
import { accountKeys } from "./accountKeys";

export function useAccountTheme(userId: string) {
  const queryClient = useQueryClient();
  const { data: currentTheme, isLoading } = useUsersGetTheme(userId);
  const setThemeMutation = useUsersSetTheme({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountKeys.theme(userId),
        });
      },
    },
  });

  const [selected, setSelected] = useState(currentTheme ?? "system");
  const [success, setSuccess] = useState(false);

  const save = useCallback(
    async (theme: string) => {
      setSuccess(false);
      await setThemeMutation.mutateAsync({
        userId,
        data: { theme },
      });
      setSuccess(true);
    },
    [setThemeMutation, userId]
  );

  return {
    currentTheme,
    selected,
    setSelected,
    success,
    setSuccess,
    isLoading,
    isError: setThemeMutation.isError,
    isPending: setThemeMutation.isPending,
    save,
  };
}
