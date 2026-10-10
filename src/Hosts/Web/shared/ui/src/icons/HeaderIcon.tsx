import React from "react";
import { FaIcon } from "./FaIcon";

/** Semantic theme color a header icon tile is painted with; `inverse` sits on an accent fill. */
export type HeaderIconTone = "accent" | "success" | "warning" | "danger" | "inverse";

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
    accent: "bg-accent text-accent-foreground shadow-accent/30",
    success: "bg-success text-success-foreground shadow-success/30",
    warning: "bg-warning text-warning-foreground shadow-warning/30",
    danger: "bg-danger text-danger-foreground shadow-danger/30",
    inverse: "bg-accent-foreground/20 text-accent-foreground shadow-none",
  },
  section: {
    accent: "bg-accent-soft text-accent",
    success: "bg-success-soft text-success",
    warning: "bg-warning-soft text-warning",
    danger: "bg-danger-soft text-danger",
    inverse: "bg-accent-foreground/20 text-accent-foreground",
  },
};

/** Decorative theme-colored icon tile placed before a page or section heading. */
export function HeaderIcon({ icon, level = "section", tone = "accent", className }: HeaderIconProps) {
  return (
    <span
      aria-hidden="true"
      className={`flex shrink-0 items-center justify-center ${levelClassNames[level]} ${toneClassNames[level][tone]} ${className ?? ""}`.trim()}
    >
      <FaIcon icon={icon} aria-hidden="true" />
    </span>
  );
}
