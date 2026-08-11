import { useState } from "react";
import { Button, Card, Chip, Tooltip } from "@heroui/react";
import { PromptSuggestion, Timeline } from "@heroui-pro/react";
import {
  ContactForm,
  submitContactInquiry,
} from "@vitarish/communications-web";
import {
  ChatConversation,
  ChatLoadingMessage,
  ChatMessage,
  FaIcon,
} from "@vitarish/web-ui";
import { AgentConversationComposer } from "@vitarish/intelligence-web";
import { useLandingConversation } from "../conversation/use-landing-conversation";
import type { LandingConversationOwner } from "../conversation/landing-conversation";

type Translate = (key: string) => string;

export function GuidedPage({
  direction,
  path,
  t,
}: {
  direction: "ltr" | "rtl";
  path: string;
  t: Translate;
}) {
  const page = path.slice(1);
  if (page === "contact") {
    return (
      <div className="mx-auto w-full max-w-2xl px-4 pt-10 sm:px-6">
        <Card>
          <Card.Header>
            <Card.Title>{t("landing.pages.contact.title")}</Card.Title>
            <Card.Description>
              {t("landing.pages.contact.description")}
            </Card.Description>
          </Card.Header>
          <Card.Content>
            <ContactForm
              context="vitarish-landing"
              onSubmit={submitContactInquiry}
            />
          </Card.Content>
        </Card>
        <PageConversation direction={direction} page={page} t={t} />
      </div>
    );
  }

  const items =
    page === "portfolio"
      ? ["portfolio0", "portfolio1", "portfolio2"]
      : page === "about"
        ? ["about0", "about1", "about2"]
        : ["explore0", "explore1", "explore2"];
  return (
    <div className="mx-auto w-full max-w-3xl px-4 pb-10 pt-10 sm:px-6">
      <div className="mb-8">
        <Chip color="accent" variant="soft">
          {t(`landing.pages.${page}.eyebrow`)}
        </Chip>
        <h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">
          {t(`landing.pages.${page}.title`)}
        </h1>
        <p className="mt-3 max-w-2xl text-muted">
          {t(`landing.pages.${page}.description`)}
        </p>
      </div>
      <Timeline density="compact" size="sm">
        {items.map((item, index) => (
          <Timeline.Item
            key={item}
            status={index === 0 ? "current" : "default"}
          >
            <Timeline.Marker aria-hidden="true">
              <FaIcon
                icon={
                  index === 0
                    ? "sparkles"
                    : index === 1
                      ? "arrow-right"
                      : "heart"
                }
              />
            </Timeline.Marker>
            <Timeline.Content>
              <Card variant={index === 0 ? "secondary" : "default"}>
                <Card.Header>
                  <Card.Title>
                    {t(`landing.pages.${page}.${item}.title`)}
                  </Card.Title>
                  <Card.Description>
                    {t(`landing.pages.${page}.${item}.description`)}
                  </Card.Description>
                </Card.Header>
              </Card>
            </Timeline.Content>
          </Timeline.Item>
        ))}
      </Timeline>
      <PageConversation direction={direction} page={page} t={t} />
    </div>
  );
}

function PageConversation({
  direction,
  page,
  t,
}: {
  direction: "ltr" | "rtl";
  page: string;
  t: Translate;
}) {
  const [value, setValue] = useState("");
  const conversation = useLandingConversation(page as LandingConversationOwner);
  const ask = async (question: string) => {
    if (!question.trim() || conversation.isPending) return;
    setValue("");
    await conversation.submit(question);
  };

  return (
    <Card className="mt-10" variant="secondary">
      <Card.Header>
        <Card.Title>{t("landing.pages.faq.title")}</Card.Title>
        <Card.Description>
          {t("landing.pages.faq.description")}
        </Card.Description>
      </Card.Header>
      <Card.Content>
        <ChatConversation
          className="max-h-64 overflow-y-auto"
          innerClassName="flex flex-col gap-3"
        >
          {conversation.messages.length === 0 ? (
            <PromptSuggestion>
              <PromptSuggestion.Group
                description={t("landing.pages.faq.description")}
                label={t("landing.pages.faq.questions")}
              >
                <PromptSuggestion.Items>
                  {[0, 1].map((index) => {
                    const question = t(`landing.pages.${page}.faq${index}`);
                    return (
                      <PromptSuggestion.Item
                        key={index}
                        aria-label={question}
                        onPress={() => void ask(question)}
                      >
                        {question}
                      </PromptSuggestion.Item>
                    );
                  })}
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
                  avatarAlt={t("landing.app")}
                  avatarFallback="V"
                  kind="answer"
                  title={t("landing.app")}
                >
                  {message.text}
                </ChatMessage>
              ),
            )
          )}
          {conversation.isPending ? (
            <ChatLoadingMessage
              avatarAlt={t("landing.app")}
              avatarFallback="V"
              loadingLabel={t("landing.thinking")}
              message={t("landing.thinkingBody")}
              title={t("landing.thinking")}
            />
          ) : null}
        </ChatConversation>
      </Card.Content>
      <Card.Footer className="flex-col gap-2">
        <div className="flex w-full justify-end gap-2">
          {conversation.isError ? (
            <Tooltip>
              <Button
                aria-label={t("landing.retry")}
                size="sm"
                variant="ghost"
                onPress={() => void conversation.retry()}
              >
                <FaIcon aria-hidden="true" icon="rotate-right" />
                {t("landing.retry")}
              </Button>
              <Tooltip.Content>{t("landing.retryTooltip")}</Tooltip.Content>
            </Tooltip>
          ) : null}
          <Tooltip>
            <Button
              aria-label={t("landing.reset")}
              size="sm"
              variant="ghost"
              onPress={() => void conversation.reset()}
            >
              <FaIcon aria-hidden="true" icon="trash-can" />
              {t("landing.reset")}
            </Button>
            <Tooltip.Content>{t("landing.resetTooltip")}</Tooltip.Content>
          </Tooltip>
        </div>
        <AgentConversationComposer
          direction={direction}
          helperText={conversation.isError ? t("landing.error") : undefined}
          isPending={conversation.isPending}
          label={t("landing.pages.faq.label")}
          placeholder={t("landing.pages.faq.placeholder")}
          submitLabel={t("landing.pages.faq.send")}
          preferredLanguage={direction === "rtl" ? "fa-IR" : "en-US"}
          sessionKey={`landing:${page}`}
          value={value}
          onChange={setValue}
          onSubmit={(question) => void ask(question)}
        />
      </Card.Footer>
    </Card>
  );
}
