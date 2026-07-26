"use client";

import { useCallback, useMemo } from "react";
import { Button, Tooltip } from "@heroui/react";
import { usePathname, useRouter } from "next/navigation";
import { useLocalizedText } from "@/localization";
import { FaIcon } from "@vitarish/web-ui";
import type {
  DashboardShellItem,
  DashboardShellNavGroup,
} from "./DashboardShell";

interface BreadcrumbUpButtonProps {
  navGroups: readonly DashboardShellNavGroup[];
}

export function BreadcrumbUpButton({ navGroups }: BreadcrumbUpButtonProps) {
  const pathname = usePathname();
  const router = useRouter();
  const { direction, t } = useLocalizedText();

  const navigate = useCallback((href: string) => router.push(href), [router]);
  const sidebarHrefs = useMemo(
    () => collectSidebarHrefs(navGroups),
    [navGroups],
  );

  const parentHref = getParentPathname(pathname);
  const href =
    parentHref && !sidebarHrefs.has(parentHref) ? parentHref : undefined;

  if (!href) return null;

  return (
    <Tooltip>
      <Button
        aria-label={t("appShell.navigation.up")}
        isIconOnly
        variant="secondary"
        onPress={() => navigate(href)}
      >
        <FaIcon icon={direction === "rtl" ? "level-up-alt" : "level-up-alt"} />
      </Button>
      <Tooltip.Content>{t("appShell.navigation.up")}</Tooltip.Content>
    </Tooltip>
  );
}

/** Derive the parent path by stripping the last segment. Returns undefined at root or single-segment paths. */
function getParentPathname(pathname: string): string | undefined {
  const segments = pathname.split("/").filter(Boolean);
  if (segments.length < 2) return undefined;
  return "/" + segments.slice(0, -1).join("/");
}

function collectSidebarHrefs(
  groups: readonly DashboardShellNavGroup[],
): Set<string> {
  const hrefs = new Set<string>();
  const walk = (items: readonly DashboardShellItem[]) => {
    for (const item of items) {
      hrefs.add(item.href);
      if (item.children) walk(item.children);
    }
  };
  for (const group of groups) walk(group.items);
  return hrefs;
}
