"use client";

import { Card, Link } from "@heroui/react";
import { siteContent } from "../../content";

export default function ContactPage() {
  return (
    <Card className="w-full max-w-md">
      <Card.Header>
        <h1 className="text-2xl font-semibold">Contact</h1>
      </Card.Header>
      <Card.Footer className="flex flex-wrap gap-6">
        {siteContent.contact.map(({ label, href }) => (
          <Link key={href} href={href}>
            {label}
          </Link>
        ))}
      </Card.Footer>
    </Card>
  );
}
