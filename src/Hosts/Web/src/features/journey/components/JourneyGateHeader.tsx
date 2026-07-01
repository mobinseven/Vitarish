interface JourneyGateHeaderProps {
  eyebrow?: string;
  title: string;
  description?: string;
  visual?: React.ReactNode;
}

export function JourneyGateHeader({
  eyebrow,
  title,
  description,
  visual,
}: JourneyGateHeaderProps) {
  return (
    <div className="flex flex-col gap-3 text-start">
      {visual ? <div aria-hidden="true">{visual}</div> : null}
      {eyebrow ? (
        <p className="text-sm font-medium uppercase tracking-wide text-accent">
          {eyebrow}
        </p>
      ) : null}
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold tracking-tight text-foreground md:text-3xl">
          {title}
        </h1>
        {description ? (
          <p className="text-base leading-7 text-foreground/70">{description}</p>
        ) : null}
      </div>
    </div>
  );
}
