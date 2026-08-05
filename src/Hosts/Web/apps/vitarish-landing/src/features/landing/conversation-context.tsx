import { Chip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import type { LandingCopy } from "./landing-copy";

export function ConversationContext({ copy }: { copy: LandingCopy }) {
  return (
    <div className="flex h-full flex-col gap-6 p-5">
      <div>
        <div className="flex items-center justify-between gap-3">
          <p className="text-sm font-semibold">{copy.context}</p>
          <Chip color="success" size="sm" variant="soft">
            {copy.ready}
          </Chip>
        </div>
        <p className="mt-3 text-sm leading-6 text-muted">{copy.contextBody}</p>
      </div>
      <div className="border-t border-default" />
      <div className="space-y-3 text-sm text-muted">
        <p className="flex items-center gap-2">
          <FaIcon aria-hidden="true" icon="laptop" />
          {copy.contextStatus}
        </p>
        <p className="flex items-center gap-2">
          <FaIcon aria-hidden="true" icon="shield-halved" />
          {copy.privateWorkspace}
        </p>
      </div>
    </div>
  );
}
