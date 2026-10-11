"use client";

import { Button } from "@heroui/react";
import { Navbar } from "@heroui-pro/react/navbar";
import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";
import { useAuthSession } from "@vitarish/auth";

export function LandingNavbar() {
  const router = useRouter();
  const { isAuthenticated } = useAuthSession();
  const { t } = useLocalizedText();

  return (
    <div className="sticky top-0 z-40 -mb-16">
      <Navbar aria-label={t("practiceLanding.navLabel")} className="bg-transparent text-white">
        <Navbar.Header>
          <Navbar.Brand className="text-xs font-normal uppercase tracking-[0.35em] text-white">
            {t("appShell.brand")}
          </Navbar.Brand>
          <Navbar.Spacer />
          <Navbar.Content>
            <Button
              size="sm"
              variant="tertiary"
              onPress={() =>
                router.push(
                  isAuthenticated ? Routes.Clinic.Index : Routes.Login.Index,
                )
              }
            >
              {t("common.actions.start")}
            </Button>
          </Navbar.Content>
        </Navbar.Header>
      </Navbar>
    </div>
  );
}
