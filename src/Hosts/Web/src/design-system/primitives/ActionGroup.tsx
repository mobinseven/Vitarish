import type { ComponentPropsWithoutRef, ReactNode } from "react";
import { classNames } from "./classNames";

interface ActionGroupProps extends ComponentPropsWithoutRef<"div"> {
  children: ReactNode;
}

export function ActionGroup({
  children,
  className,
  ...props
}: ActionGroupProps) {
  return (
    <div className={classNames("ds-action-group", className)} {...props}>
      {children}
    </div>
  );
}
