"use client";

import { useState, useCallback } from "react";
import { Button, Link } from "@heroui/react";
import { useTheme } from "next-themes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import {
  useUserThemeQuery,
  useSetUserThemeMutation,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";
import { OptionSelector } from "@/components/account/OptionSelector";

const themes = [
  { value: "light", label: "Light" },
  { value: "dark", label: "Dark" },
  { value: "system", label: "System" },
];

export default function ThemePage() {
  const { user } = useAuthSession();
  const userId = user?.userId ?? "";
  const { setTheme: setNextTheme } = useTheme();
  const { data: currentTheme } = useUserThemeQuery(userId);
  const setTheme = useSetUserThemeMutation(userId);

  const [selected, setSelected] = useState(currentTheme ?? "system");
  const [success, setSuccess] = useState(false);

  const handleSave = useCallback(async () => {
    setSuccess(false);
    await setTheme.mutateAsync({ theme: selected });
    setNextTheme(selected);
    setSuccess(true);
  }, [selected, setTheme, setNextTheme]);

  return (
    <div className="max-w-md mx-auto flex flex-col gap-4">
      <OptionSelector
        title="Theme"
        options={themes}
        selected={selected}
        onSelect={setSelected}
      />
      {setTheme.isError && (
        <StatusMessage message="Failed to update theme" />
      )}
      {success && (
        <StatusMessage message="Theme updated successfully" type="success" />
      )}
      <Button
        onPress={handleSave}
        isPending={setTheme.isPending}
        className="w-full"
      >
        Save
      </Button>
      <Link href="/account" className="text-sm">
        Back to settings
      </Link>
    </div>
  );
}
