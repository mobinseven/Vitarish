import { Card } from "@heroui/react";

interface JourneyGateCardProps {
  header: React.ReactNode;
  children: React.ReactNode;
  actions?: React.ReactNode;
}

export function JourneyGateCard({ header, children, actions }: JourneyGateCardProps) {
  return (
    <Card className="glass-card w-full">
      <Card.Header className="flex flex-col gap-2 p-6 pb-3">{header}</Card.Header>
      <Card.Content className="flex flex-col gap-5 p-6 pt-3">{children}</Card.Content>
      {actions ? <Card.Footer className="p-6 pt-0">{actions}</Card.Footer> : null}
    </Card>
  );
}
