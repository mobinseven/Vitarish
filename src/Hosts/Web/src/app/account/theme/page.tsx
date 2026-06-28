"use client";

import { useState, useCallback } from "react";
import { Card, CardBody, CardHeader, Button, Link } from "@heroui/react";
import { useTheme } from "next-themes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import {
  useUserThemeQuery,
  useSetUserThemeMutation,
} from "@/api/generated/users/users";
import { StatusMessage } from "@/components/auth/StatusMessage";

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
    <div className="max-w-md mx-auto">
      <Card>
        <CardHeader>
          <h1 className="text-2xl font-bold">Theme</h1>
        </CardHeader>
        <CardBody className="flex flex-col gap-4">
          {setTheme.isError && (
            <StatusMessage message="Failed to update theme" />
          )}
          {success && (
            <StatusMessage message="Theme updated successfully" />
          )}

          <div className="flex flex-col gap-2">
            {themes.map((theme) => (
              <Button
                key={theme.value}
                variant={selected === theme.value ? "solid" : "flat"}
                color={selected === theme.value ? "primary" : "default"}
                onPress={() => setSelected(theme.value)}
                className="w-full justify-start"
              >
                {theme.label}
              </Button>
            ))}
          </div>

          <Button
            color="primary"
            onPress={handleSave}
            isLoading={setTheme.isPending}
            className="w-full"
          >
            Save
          </Button>

          <Link href="/account" className="text-sm">
            Back to settings
          </Link>
        </CardBody>
      </Card>
    </div>
  );
}
