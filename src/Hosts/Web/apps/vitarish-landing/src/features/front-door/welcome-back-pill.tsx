"use client";

import Link from "next/link";
import { useAuthSession } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { LandingRoutes } from "../../app/routes";

/** For a signed-in visitor, the front door's way back to their platform home; nothing for anyone else. */
export function WelcomeBackPill() {
  const { t } = useLocalizedText();
  const { isAuthenticated, displayProfile } = useAuthSession();
  if (!isAuthenticated) return null;

  const name = displayProfile.givenName;
  return (
    <Link
      href={LandingRoutes.PlatformHome}
      className="ms-3 inline-flex items-center gap-2 rounded-full border border-default bg-surface px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-surface-secondary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus"
    >
      <span>{name ? t("frontDoor.welcomeBack", { name }) : t("frontDoor.welcomeBackAnonymous")}</span>
      <span aria-hidden="true" className="text-muted">·</span>
      <span>{t("frontDoor.goHome")}</span>
      <FaIcon icon="arrow-right" aria-hidden="true" className="rtl:-scale-x-100" />
    </Link>
  );
}
