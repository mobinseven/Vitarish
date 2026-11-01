"use client";

import { Card } from "@heroui/react";
import { siteContent } from "../content";

export default function HomePage() {
  return (
    <Card className="w-full max-w-md">
      <Card.Header>
        <h1 className="text-2xl font-semibold">{siteContent.name}</h1>
        <Card.Description>{siteContent.intro}</Card.Description>
      </Card.Header>
    </Card>
  );
}
