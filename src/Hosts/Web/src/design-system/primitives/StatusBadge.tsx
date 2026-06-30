import type { ComponentPropsWithoutRef } from "react";
import { classNames } from "./classNames";

interface StatusBadgeProps extends ComponentPropsWithoutRef<"span"> {
  children: string;
  tone?: "accent" | "success" | "muted";
}

export function StatusBadge({
  children,
  tone = "accent",
  className,
  ...props
}: StatusBadgeProps) {
  return (
    <span
      className={classNames(
        "ds-status-badge",
        `ds-status-badge-${tone}`,
        className,
      )}
      {...props}
    >
      <span aria-hidden="true" className="ds-status-dot" />
      {children}
    </span>
  );
}
