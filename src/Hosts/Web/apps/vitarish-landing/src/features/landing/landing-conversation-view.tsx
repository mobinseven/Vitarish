import {
  ChatConversation,
  ChatLoadingMessage,
  ChatMessage,
  FaIcon,
} from "@vitarish/ui";
import { PromptSuggestion } from "@heroui-pro/react";
import type { LandingCopy } from "./landing-copy";

type Message = { id: number; kind: "question" | "answer"; text: string };

export function LandingConversationView({
  copy,
  conversation,
  submit,
}: {
  copy: LandingCopy;
  conversation: { isPending: boolean; messages: Message[] };
  submit: (text: string) => Promise<void>;
}) {
  return (
    <ChatConversation
      className="min-h-0 flex-1"
      innerClassName="mx-auto flex w-full max-w-[714px] flex-col gap-8 px-4 pb-8 pt-8 sm:px-6"
    >
      {conversation.messages.length === 0 ? (
        <PromptSuggestion>
          <PromptSuggestion.Header>
            <div className="mb-5 flex justify-center">
              <div className="grid size-12 place-items-center rounded-2xl bg-accent text-accent-foreground shadow-sm">
                <FaIcon
                  aria-hidden="true"
                  className="text-xl"
                  icon="sparkles"
                />
              </div>
            </div>
            <PromptSuggestion.Title>{copy.chatTitle}</PromptSuggestion.Title>
            <PromptSuggestion.Description>
              {copy.intro}
            </PromptSuggestion.Description>
          </PromptSuggestion.Header>
          <PromptSuggestion.Group
            description={copy.suggestionDescription}
            label={copy.suggestionTitle}
          >
            <PromptSuggestion.Items>
              {copy.suggestions.map((suggestion) => (
                <PromptSuggestion.Item
                  key={suggestion.text}
                  aria-label={suggestion.text}
                  onPress={() => void submit(suggestion.text)}
                >
                  <FaIcon aria-hidden="true" icon={suggestion.icon} />
                  {suggestion.text}
                </PromptSuggestion.Item>
              ))}
            </PromptSuggestion.Items>
          </PromptSuggestion.Group>
        </PromptSuggestion>
      ) : (
        conversation.messages.map((message) =>
          message.kind === "question" ? (
            <ChatMessage key={message.id} kind="question">
              {message.text}
            </ChatMessage>
          ) : (
            <ChatMessage
              key={message.id}
              avatarAlt={copy.app}
              avatarFallback="V"
              kind="answer"
              title={copy.app}
            >
              {message.text}
            </ChatMessage>
          ),
        )
      )}
      {conversation.isPending ? (
        <ChatLoadingMessage
          avatarAlt={copy.app}
          avatarFallback="V"
          loadingLabel={copy.thinking}
          message={copy.thinkingBody}
          title={copy.thinking}
        />
      ) : null}
    </ChatConversation>
  );
}
