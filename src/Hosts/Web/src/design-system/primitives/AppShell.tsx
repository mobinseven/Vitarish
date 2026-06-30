import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { classNames } from "./classNames";

interface AppShellProps extends ComponentPropsWithoutRef<"main"> {
  children: ReactNode;
  maxWidth?: "content" | "wide";
}

export function AppShell({
  children,
  className,
  maxWidth = "wide",
  ...props
}: AppShellProps) {
  return (
    <main className={classNames("ds-app-shell", className)} {...props}>
      <div
        className={classNames(
          "ds-page-container",
          maxWidth === "content" && "ds-page-container-content",
        )}
      >
        {children}
      </div>
    </main>
  );
}
