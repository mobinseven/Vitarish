"use client";

import { Accordion } from "@heroui/react";
import { siteContent } from "../../content";

export default function WorkPage() {
  return (
    <Accordion aria-label="Work" className="w-full max-w-md" variant="surface">
      {siteContent.work.map(({ title, description }) => (
        <Accordion.Item key={title}>
          <Accordion.Heading>
            <Accordion.Trigger>
              {title}
              <Accordion.Indicator />
            </Accordion.Trigger>
          </Accordion.Heading>
          <Accordion.Panel>
            <Accordion.Body>{description}</Accordion.Body>
          </Accordion.Panel>
        </Accordion.Item>
      ))}
    </Accordion>
  );
}
