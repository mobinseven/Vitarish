import { useState, useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useUsersGetCulture, useUsersSetCulture } from "@vitarish/auth";
import { accountKeys } from "./accountKeys";

export function useAccountCulture(userId: string) {
  const queryClient = useQueryClient();
  const { data: currentCulture, isLoading } = useUsersGetCulture(userId);
  const setCultureMutation = useUsersSetCulture({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: accountKeys.culture(userId),
        });
      },
    },
  });

  const [selected, setSelected] = useState("en-US");
  const [syncedCulture, setSyncedCulture] = useState<string | undefined>();
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (currentCulture && currentCulture !== syncedCulture) {
      setSelected(currentCulture);
      setSyncedCulture(currentCulture);
    }
  }, [currentCulture, syncedCulture]);

  const save = useCallback(
    async (culture: string) => {
      setSuccess(false);
      await setCultureMutation.mutateAsync({
        userId,
        data: { culture },
      });
      setSuccess(true);
    },
    [setCultureMutation, userId]
  );

  return {
    currentCulture,
    selected,
    setSelected,
    success,
    setSuccess,
    isLoading,
    isError: setCultureMutation.isError,
    isPending: setCultureMutation.isPending,
    save,
  };
}
