"use client";

import TechText from "./TechText";

export interface LoadingSpinnerProps {
  className?: string;
}

const brandWordmark = "VITARISH";

export function LoadingSpinner({ className }: LoadingSpinnerProps) {
  return (
    <div className={["h-56 w-[min(90vw,48rem)] text-foreground", className].filter(Boolean).join(" ")}>
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
      />
    </div>
  );
}
