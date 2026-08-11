import { useState } from "react";
import { Button, Tooltip } from "@heroui/react";
import { AgentConversationComposer } from "@vitarish/intelligence-web";
import { FaIcon } from "@vitarish/web-ui";
import type { LandingCopy } from "./landing-copy";

type ConversationState = {
  isError: boolean;
  isPending: boolean;
  messages: unknown[];
  reset: () => Promise<void>;
  retry: () => Promise<void>;
};

export function LandingComposer({
  copy,
  conversation,
  direction,
  submit,
}: {
  copy: LandingCopy;
  conversation: ConversationState;
  direction: "ltr" | "rtl";
  submit: (text: string) => Promise<void>;
}) {
  const [value, setValue] = useState("");

  return (
    <div className="shrink-0 bg-gradient-to-t from-background via-background to-transparent px-4 pb-4 pt-3 sm:px-6 sm:pb-6">
      <div className="mx-auto w-full max-w-[714px]">
        {conversation.isError || conversation.messages.length > 0 ? (
          <div className="mb-2 flex justify-end gap-2">
            {conversation.isError ? (
              <Tooltip>
                <Button
                  aria-label={copy.retry}
                  size="sm"
                  variant="ghost"
                  onPress={() => void conversation.retry()}
                >
                  <FaIcon aria-hidden="true" icon="rotate-right" />
                  {copy.retry}
                </Button>
                <Tooltip.Content>{copy.retryTooltip}</Tooltip.Content>
              </Tooltip>
            ) : null}
            <Tooltip>
              <Button
                aria-label={copy.reset}
                size="sm"
                variant="ghost"
                onPress={() => void conversation.reset()}
              >
                <FaIcon aria-hidden="true" icon="trash-can" />
                {copy.reset}
              </Button>
              <Tooltip.Content>{copy.resetTooltip}</Tooltip.Content>
            </Tooltip>
          </div>
        ) : null}
        <AgentConversationComposer
          direction={direction}
          footer={
            <span className="text-center text-xs text-muted">
              {copy.disclaimer}
            </span>
          }
          helperIcon={<FaIcon aria-hidden="true" icon="sparkles" />}
          helperText={
            conversation.isPending
              ? copy.thinking
              : conversation.isError
                ? copy.error
                : copy.ready
          }
          isPending={conversation.isPending}
          label={copy.promptLabel}
          placeholder={copy.placeholder}
          submitLabel={copy.send}
          preferredLanguage={direction === "rtl" ? "fa-IR" : "en-US"}
          sessionKey="landing-composer"
          value={value}
          onChange={setValue}
          onSubmit={async (text) => {
            setValue("");
            await submit(text);
          }}
        />
      </div>
    </div>
  );
}
