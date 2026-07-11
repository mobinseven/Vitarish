import { useState, useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useUsersGetTheme, useUsersSetTheme } from "@vitarish/auth-web";
import { accountKeys } from "./accountKeys";
import {
  decodeAccountThemePreference,
  encodeAccountThemePreference,
  type AccountThemePreference,
  type AccountThemeValue,
} from "../components/theme/themeOptions";

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

  const [selected, setSelected] = useState<AccountThemeValue>("default");
  const [isVibrant, setIsVibrant] = useState(false);
  const [syncedTheme, setSyncedTheme] = useState<string | undefined>();
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (currentTheme && currentTheme !== syncedTheme) {
      const preference = decodeAccountThemePreference(currentTheme);

      setSelected(preference.theme);
      setIsVibrant(preference.isVibrant);
      setSyncedTheme(currentTheme);
    }
  }, [currentTheme, syncedTheme]);

  const save = useCallback(
    async (preference: AccountThemePreference) => {
      setSuccess(false);
      await setThemeMutation.mutateAsync({
        userId,
        data: { theme: encodeAccountThemePreference(preference) },
      });
      setSuccess(true);
    },
    [setThemeMutation, userId]
  );

  return {
    currentTheme,
    selected,
    setSelected,
    isVibrant,
    setIsVibrant,
    success,
    setSuccess,
    isLoading,
    isError: setThemeMutation.isError,
    isPending: setThemeMutation.isPending,
    save,
  };
}
