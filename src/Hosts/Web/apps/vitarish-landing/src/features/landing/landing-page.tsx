"use client";

import { useMemo, useState } from "react";
import { Button, Card, Chip, Tooltip } from "@heroui/react";
import { PromptSuggestion, Timeline } from "@heroui-pro/react";
import { usePathname, useRouter } from "next/navigation";
import { ContactForm, submitContactInquiry } from "@vitarish/communications-web";
import {
  ChatConversation,
  ChatLoadingMessage,
  ChatMessage,
  ChatPromptInput,
  DashboardFrame,
  FaIcon,
  ImmersiveRegion,
} from "@vitarish/web-ui";
import { useLocalizedText } from "@vitarish/web-localization";
import { useLandingConversation } from "../conversation/use-landing-conversation";
import type { LandingConversationOwner } from "../conversation/landing-conversation";

type LandingCopy = {
  context: string;
  ready: string;
  contextBody: string;
  contextStatus: string;
  privateWorkspace: string;
};

const guidedPaths = ["/about", "/portfolio", "/contact", "/explore"] as const;

export function LandingPage() {
  const { direction, t: translate } = useLocalizedText();
  const pathname = usePathname();
  const router = useRouter();
  const t = {
    app: translate("landing.app"), subtitle: translate("landing.subtitle"), workspace: translate("landing.workspace"), recent: translate("landing.recent"),
    newChat: translate("landing.newChat"), about: translate("landing.about"), portfolio: translate("landing.portfolio"), contact: translate("landing.contact"), library: translate("landing.library"), explore: translate("landing.explore"),
    chatTitle: translate("landing.chatTitle"), intro: translate("landing.intro"), promptLabel: translate("landing.promptLabel"), placeholder: translate("landing.placeholder"), send: translate("landing.send"), ready: translate("landing.ready"), thinking: translate("landing.thinking"), thinkingBody: translate("landing.thinkingBody"),
    context: translate("landing.context"), contextBody: translate("landing.contextBody"), contextStatus: translate("landing.contextStatus"), privateWorkspace: translate("landing.privateWorkspace"), disclaimer: translate("landing.disclaimer"), skip: translate("landing.skip"),
    suggestionTitle: translate("landing.suggestionTitle"), suggestionDescription: translate("landing.suggestionDescription"),
    suggestions: [
      { icon: "compass", text: translate("landing.suggestion0") },
      { icon: "lightbulb", text: translate("landing.suggestion1") },
      { icon: "list-check", text: translate("landing.suggestion2") },
      { icon: "pen-to-square", text: translate("landing.suggestion3") },
    ],
    error: translate("landing.error"), retry: translate("landing.retry"), reset: translate("landing.reset"), retryTooltip: translate("landing.retryTooltip"), resetTooltip: translate("landing.resetTooltip"),
  };
  const currentPath = pathname === "/" ? "/" : pathname;
  const [value, setValue] = useState("");
  const conversation = useLandingConversation("home");

  const navigationGroups = useMemo(
    () => [
      {
        label: t.workspace,
        items: [
          { exactMatch: true, href: "/", icon: "pen-to-square", label: t.newChat },
          { href: "/about", icon: "heart", label: t.about },
          { href: "/explore", icon: "compass", label: t.explore },
        ],
      },
      {
        label: t.recent,
        items: [
          { href: "/portfolio", icon: "images", label: t.portfolio },
          { href: "/contact", icon: "envelope", label: t.contact },
        ],
      },
    ],
    [t],
  );

  function openPath(path: string) {
    conversation.reset();
    router.push(path);
    setValue("");
  }

  async function submit(text: string) {
    if (!text.trim() || conversation.isPending) return;
    setValue("");
    await conversation.submit(text);
  }

  return (
    <DashboardFrame
      aside={<ConversationContext copy={t} />}
      asideMobile="sheet"
      contentMode="workspace"
      currentPath={currentPath}
      footerItems={[]}
      identity={{
        accountHref: "/",
        accountLabel: t.app,
        initials: "V",
        label: t.app,
        subtitle: t.subtitle,
      }}
      mobileAside={<ConversationContext copy={t} />}
      mobileNavigation="sidebar"
      navigate={openPath}
      navigationGroups={navigationGroups}
      navigationLabel={t.workspace}
      skipLinkLabel={t.skip}
      title={<span className="font-semibold">{t.app}</span>}
      toolbar={null}
    >
      <ImmersiveRegion className="flex min-h-0 flex-1 flex-col bg-background">
        <ChatConversation
          className="min-h-0 flex-1"
          innerClassName="mx-auto flex w-full max-w-[714px] flex-col gap-8 px-4 pb-8 pt-8 sm:px-6"
        >
          {guidedPaths.includes(currentPath as (typeof guidedPaths)[number]) ? (
            <GuidedPage direction={direction} path={currentPath} t={translate} />
          ) : conversation.messages.length === 0 ? (
            <PromptSuggestion>
              <PromptSuggestion.Header>
                <div className="mb-5 flex justify-center">
                  <div className="grid size-12 place-items-center rounded-2xl bg-accent text-accent-foreground shadow-sm">
                    <FaIcon aria-hidden="true" className="text-xl" icon="sparkles" />
                  </div>
                </div>
                <PromptSuggestion.Title>{t.chatTitle}</PromptSuggestion.Title>
                <PromptSuggestion.Description>{t.intro}</PromptSuggestion.Description>
              </PromptSuggestion.Header>
              <PromptSuggestion.Group description={t.suggestionDescription} label={t.suggestionTitle}>
                <PromptSuggestion.Items>
                  {t.suggestions.map((suggestion) => (
                    <PromptSuggestion.Item
                      key={suggestion.text}
                      aria-label={suggestion.text}
                      onPress={() => submit(suggestion.text)}
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
                  avatarAlt={t.app}
                  avatarFallback="V"
                  kind="answer"
                  title={t.app}
                >
                  {message.text}
                </ChatMessage>
              ),
            )
          )}
          {conversation.isPending ? (
            <ChatLoadingMessage
              avatarAlt={t.app}
              avatarFallback="V"
              loadingLabel={t.thinking}
              message={t.thinkingBody}
              title={t.thinking}
            />
          ) : null}
        </ChatConversation>

        {guidedPaths.includes(currentPath as (typeof guidedPaths)[number]) ? null : <div className="shrink-0 bg-gradient-to-t from-background via-background to-transparent px-4 pb-4 pt-3 sm:px-6 sm:pb-6">
          <div className="mx-auto w-full max-w-[714px]">
            {conversation.isError || conversation.messages.length > 0 ? <div className="mb-2 flex justify-end gap-2">{conversation.isError ? <Tooltip><Button aria-label={t.retry} size="sm" variant="ghost" onPress={() => void conversation.retry()}><FaIcon aria-hidden="true" icon="rotate-right" />{t.retry}</Button><Tooltip.Content>{t.retryTooltip}</Tooltip.Content></Tooltip> : null}<Tooltip><Button aria-label={t.reset} size="sm" variant="ghost" onPress={conversation.reset}><FaIcon aria-hidden="true" icon="trash-can" />{t.reset}</Button><Tooltip.Content>{t.resetTooltip}</Tooltip.Content></Tooltip></div> : null}
            <ChatPromptInput
              direction={direction}
              footer={<span className="text-center text-xs text-muted">{t.disclaimer}</span>}
              helperIcon={<FaIcon aria-hidden="true" icon="sparkles" />}
              helperText={conversation.isPending ? t.thinking : conversation.isError ? t.error : t.ready}
              isPending={conversation.isPending}
              label={t.promptLabel}
              placeholder={t.placeholder}
              submitLabel={t.send}
              value={value}
              onChange={setValue}
              onSubmit={submit}
            />
          </div>
        </div>}
      </ImmersiveRegion>
    </DashboardFrame>
  );
}

function GuidedPage({ direction, path, t }: { direction: "ltr" | "rtl"; path: string; t: (key: string) => string }) {
  const page = path.slice(1);
  if (page === "contact") {
    return <div className="mx-auto w-full max-w-2xl px-4 pt-10 sm:px-6"><Card><Card.Header><Card.Title>{t("landing.pages.contact.title")}</Card.Title><Card.Description>{t("landing.pages.contact.description")}</Card.Description></Card.Header><Card.Content><ContactForm context="vitarish-landing" onSubmit={submitContactInquiry} /></Card.Content></Card><PageConversation direction={direction} page={page} t={t} /></div>;
  }
  const items = page === "portfolio" ? ["portfolio0", "portfolio1", "portfolio2"] : page === "about" ? ["about0", "about1", "about2"] : ["explore0", "explore1", "explore2"];
  return <div className="mx-auto w-full max-w-3xl px-4 pb-10 pt-10 sm:px-6"><div className="mb-8"><Chip color="accent" variant="soft">{t(`landing.pages.${page}.eyebrow`)}</Chip><h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl">{t(`landing.pages.${page}.title`)}</h1><p className="mt-3 max-w-2xl text-muted">{t(`landing.pages.${page}.description`)}</p></div><Timeline density="compact" size="sm">{items.map((item, index) => <Timeline.Item key={item} status={index === 0 ? "current" : "default"}><Timeline.Marker aria-hidden="true"><FaIcon icon={index === 0 ? "sparkles" : index === 1 ? "arrow-right" : "heart"} /></Timeline.Marker><Timeline.Content><Card variant={index === 0 ? "secondary" : "default"}><Card.Header><Card.Title>{t(`landing.pages.${page}.${item}.title`)}</Card.Title><Card.Description>{t(`landing.pages.${page}.${item}.description`)}</Card.Description></Card.Header></Card></Timeline.Content></Timeline.Item>)}</Timeline><PageConversation direction={direction} page={page} t={t} /></div>;
}

function PageConversation({ direction, page, t }: { direction: "ltr" | "rtl"; page: string; t: (key: string) => string }) {
  const [value, setValue] = useState("");
  const conversation = useLandingConversation(page as LandingConversationOwner);
  const ask = async (question: string) => { if (!question.trim() || conversation.isPending) return; setValue(""); await conversation.submit(question); };
  return <Card className="mt-10" variant="secondary"><Card.Header><Card.Title>{t("landing.pages.faq.title")}</Card.Title><Card.Description>{t("landing.pages.faq.description")}</Card.Description></Card.Header><Card.Content><ChatConversation className="max-h-64 overflow-y-auto" innerClassName="flex flex-col gap-3">{conversation.messages.length === 0 ? <PromptSuggestion><PromptSuggestion.Group description={t("landing.pages.faq.description")} label={t("landing.pages.faq.questions")}><PromptSuggestion.Items>{[0,1].map((index) => { const question = t(`landing.pages.${page}.faq${index}`); return <PromptSuggestion.Item key={index} aria-label={question} onPress={() => void ask(question)}>{question}</PromptSuggestion.Item>; })}</PromptSuggestion.Items></PromptSuggestion.Group></PromptSuggestion> : conversation.messages.map((message) => message.kind === "question" ? <ChatMessage key={message.id} kind="question">{message.text}</ChatMessage> : <ChatMessage key={message.id} avatarAlt={t("landing.app")} avatarFallback="V" kind="answer" title={t("landing.app")}>{message.text}</ChatMessage>)}{conversation.isPending ? <ChatLoadingMessage avatarAlt={t("landing.app")} avatarFallback="V" loadingLabel={t("landing.thinking")} message={t("landing.thinkingBody")} title={t("landing.thinking")} /> : null}</ChatConversation></Card.Content><Card.Footer className="flex-col gap-2"><div className="flex w-full justify-end gap-2">{conversation.isError ? <Tooltip><Button aria-label={t("landing.retry")} size="sm" variant="ghost" onPress={() => void conversation.retry()}><FaIcon aria-hidden="true" icon="rotate-right" />{t("landing.retry")}</Button><Tooltip.Content>{t("landing.retryTooltip")}</Tooltip.Content></Tooltip> : null}<Tooltip><Button aria-label={t("landing.reset")} size="sm" variant="ghost" onPress={conversation.reset}><FaIcon aria-hidden="true" icon="trash-can" />{t("landing.reset")}</Button><Tooltip.Content>{t("landing.resetTooltip")}</Tooltip.Content></Tooltip></div><ChatPromptInput direction={direction} helperText={conversation.isError ? t("landing.error") : undefined} isPending={conversation.isPending} label={t("landing.pages.faq.label")} placeholder={t("landing.pages.faq.placeholder")} submitLabel={t("landing.pages.faq.send")} value={value} onChange={setValue} onSubmit={(question) => void ask(question)} /></Card.Footer></Card>;
}

function ConversationContext({
  copy,
}: {
  copy: LandingCopy;
}) {
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
