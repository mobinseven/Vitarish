"use client";

import { Card } from "@heroui/react";
import { siteContent } from "../../content";

export default function AboutPage() {
  return (
    <Card className="w-full max-w-md">
      <Card.Header>
        <h1 className="text-2xl font-semibold">About</h1>
        <Card.Description>{siteContent.about}</Card.Description>
      </Card.Header>
    </Card>
  );
}
