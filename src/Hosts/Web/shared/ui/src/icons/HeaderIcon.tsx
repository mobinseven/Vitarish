import React from "react";
import { FaIcon } from "./FaIcon";

/**
 * Color a header icon tile is painted with. Header icons are neutral; a status
 * tone marks a header whose content has that status, and `inverse` sits on an
 * accent fill.
 */
export type HeaderIconTone = "neutral" | "success" | "warning" | "danger" | "inverse";

/** `page` marks a page header with a solid tile; `section` marks a section header with a soft tile. */
export type HeaderIconLevel = "page" | "section";

export interface HeaderIconProps {
  icon: string;
  level?: HeaderIconLevel;
  tone?: HeaderIconTone;
  className?: string;
}

const levelClassNames: Record<HeaderIconLevel, string> = {
  page: "size-14 rounded-2xl text-2xl shadow-lg",
  section: "size-10 rounded-xl text-base",
};

const toneClassNames: Record<HeaderIconLevel, Record<HeaderIconTone, string>> = {
  page: {
    neutral: "bg-surface text-foreground shadow-foreground/10",
    success: "bg-success text-success-foreground shadow-success/30",
    warning: "bg-warning text-warning-foreground shadow-warning/30",
    danger: "bg-danger text-danger-foreground shadow-danger/30",
    inverse: "bg-accent-foreground/20 text-accent-foreground shadow-none",
  },
  section: {
    neutral: "bg-surface-secondary text-foreground",
    success: "bg-success-soft text-success",
    warning: "bg-warning-soft text-warning",
    danger: "bg-danger-soft text-danger",
    inverse: "bg-accent-foreground/20 text-accent-foreground",
  },
};

/** Decorative icon tile placed before a page or section heading. */
export function HeaderIcon({ icon, level = "section", tone = "neutral", className }: HeaderIconProps) {
  return (
    <span
      aria-hidden="true"
      className={`flex shrink-0 items-center justify-center ${levelClassNames[level]} ${toneClassNames[level][tone]} ${className ?? ""}`.trim()}
    >
      <FaIcon icon={icon} aria-hidden="true" />
    </span>
  );
}
