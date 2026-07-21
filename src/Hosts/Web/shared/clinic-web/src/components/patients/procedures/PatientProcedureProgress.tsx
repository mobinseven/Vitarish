"use client";

interface PatientProcedureProgressProps {
  eventCount: number;
  committedEventCount: number;
}

export function PatientProcedureProgress({
  eventCount,
  committedEventCount,
}: PatientProcedureProgressProps) {
  return (
    <div aria-hidden="true" className="flex items-center gap-0.5">
      {Array.from({ length: committedEventCount }, (_, i) => (
        <span key={`committed-${i}`} className="bg-success size-2" />
      ))}
      {Array.from({ length: eventCount - committedEventCount }, (_, i) => (
        <span key={`pending-${i}`} className="bg-separator size-2" />
      ))}
    </div>
  );
}
