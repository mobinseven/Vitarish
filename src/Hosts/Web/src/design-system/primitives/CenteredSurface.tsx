import { Card } from "@heroui/react";
import type { ReactNode } from "react";
import { SurfaceCard } from "./SurfaceCard";

interface CenteredSurfaceProps {
  children: ReactNode;
  title: string;
  description?: string;
}

export function CenteredSurface({ children, title, description }: CenteredSurfaceProps) {
  return (
    <main className="ds-app-shell ds-centered-shell">
      <SurfaceCard className="w-full max-w-md">
        <Card.Header className="flex flex-col items-center gap-2 pb-2 text-center">
          <h1 className="text-2xl font-bold">{title}</h1>
          {description ? <p className="text-sm text-muted">{description}</p> : null}
        </Card.Header>
        <Card.Content>{children}</Card.Content>
      </SurfaceCard>
    </main>
  );
}
