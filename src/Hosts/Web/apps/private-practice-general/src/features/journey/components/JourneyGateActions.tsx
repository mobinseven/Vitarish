interface JourneyGateActionsProps {
  children: React.ReactNode;
}

export function JourneyGateActions({ children }: JourneyGateActionsProps) {
  return <div className="flex flex-col gap-3 sm:flex-row sm:justify-end">{children}</div>;
}
