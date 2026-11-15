"use client";

import { useState } from "react";
import TechText from "./TechText";

export interface LoadingSpinnerProps {
  className?: string;
}

const brandWordmark = "VITARISH";

export function LoadingSpinner({ className }: LoadingSpinnerProps) {
  const [canvasPainted, setCanvasPainted] = useState(false);

  return (
    <div className={["relative h-56 w-[min(90vw,48rem)] text-foreground", className].filter(Boolean).join(" ")}>
      {!canvasPainted && (
        <span aria-hidden="true" className="pointer-events-none absolute inset-0 z-10 flex items-center justify-center">
          <span className="h-10 w-10 rounded-full border-2 border-current border-t-transparent opacity-70 motion-safe:animate-spin" />
        </span>
      )}
      <TechText
        text={brandWordmark}
        fontWeight={600}
        fontSize={150}
        reveal="letter"
        speed={4}
        dashLength={4}
        dashGap={2}
        specks={15}
        intro="none"
        introDuration={2}
        clickKick
        guides={false}
        onReady={() => setCanvasPainted(true)}
      />
    </div>
  );
}
