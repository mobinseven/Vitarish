import { Card } from "@heroui/react";
import type { ComponentPropsWithoutRef } from "react";
import { classNames } from "./classNames";

type SurfaceTone = "default" | "elevated" | "interactive";

interface SurfaceCardProps extends ComponentPropsWithoutRef<typeof Card> {
  tone?: SurfaceTone;
}

function SurfaceCardRoot({
  className,
  tone = "default",
  ...props
}: SurfaceCardProps) {
  return (
    <Card
      className={classNames(
        "ds-surface-card",
        `ds-surface-card-${tone}`,
        className,
      )}
      {...props}
    />
  );
}

export const SurfaceCard = Object.assign(SurfaceCardRoot, {
  Header: Card.Header,
  Content: Card.Content,
  Footer: Card.Footer,
});
