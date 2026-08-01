"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Button, Card, Chip } from "@heroui/react";
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

type Message = { id: number; kind: "question" | "answer"; text: string };
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
    reply: translate("landing.reply"), focusQuestion: translate("landing.focusQuestion"), focusAnswer: translate("landing.focusAnswer"), ideaQuestion: translate("landing.ideaQuestion"), ideaAnswer: translate("landing.ideaAnswer"),
  };
  const currentPath = pathname === "/" ? "/" : pathname;
  const [value, setValue] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [isPending, setIsPending] = useState(false);
  const nextMessageId = useRef(1);
  const replyTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(
    () => () => {
      if (replyTimer.current) clearTimeout(replyTimer.current);
    },
    [],
  );

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

  function cancelPendingReply() {
    if (replyTimer.current) clearTimeout(replyTimer.current);
    replyTimer.current = null;
    setIsPending(false);
  }

  function openPath(path: string) {
    cancelPendingReply();
    router.push(path);
    setValue("");

    setMessages([]);
  }

  function submit(text: string) {
    const trimmed = text.trim();
    if (!trimmed || isPending) return;

    router.push("/");
    setMessages((current) => [
      ...current,
      { id: nextMessageId.current++, kind: "question", text: trimmed },
    ]);
    setValue("");
    setIsPending(true);

    replyTimer.current = setTimeout(() => {
      setMessages((current) => [
        ...current,
        {
          id: nextMessageId.current++,
          kind: "answer",
          text: t.reply,
        },
      ]);
      setIsPending(false);
      replyTimer.current = null;
    }, 650);
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
          ) : messages.length === 0 ? (
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
            messages.map((message) =>
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
          {isPending ? (
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
            <ChatPromptInput
              direction={direction}
              footer={<span className="text-center text-xs text-muted">{t.disclaimer}</span>}
              helperIcon={<FaIcon aria-hidden="true" icon="sparkles" />}
              helperText={isPending ? t.thinking : t.ready}
              isPending={isPending}
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
  const [messages, setMessages] = useState<Message[]>([]);
  const ask = (question: string) => { if (!question.trim()) return; setMessages((current) => [...current, { id: Date.now(), kind: "question", text: question }, { id: Date.now() + 1, kind: "answer", text: t(`landing.pages.${page}.faqAnswer`) }]); setValue(""); };
  return <Card className="mt-10" variant="secondary"><Card.Header><Card.Title>{t("landing.pages.faq.title")}</Card.Title><Card.Description>{t("landing.pages.faq.description")}</Card.Description></Card.Header><Card.Content><ChatConversation className="max-h-64 overflow-y-auto" innerClassName="flex flex-col gap-3">{messages.length === 0 ? <PromptSuggestion><PromptSuggestion.Group description={t("landing.pages.faq.description")} label={t("landing.pages.faq.questions")}><PromptSuggestion.Items>{[0,1].map((index) => { const question = t(`landing.pages.${page}.faq${index}`); return <PromptSuggestion.Item key={index} aria-label={question} onPress={() => ask(question)}>{question}</PromptSuggestion.Item>; })}</PromptSuggestion.Items></PromptSuggestion.Group></PromptSuggestion> : messages.map((message) => message.kind === "question" ? <ChatMessage key={message.id} kind="question">{message.text}</ChatMessage> : <ChatMessage key={message.id} avatarAlt={t("landing.app")} avatarFallback="V" kind="answer" title={t("landing.app")}>{message.text}</ChatMessage>)}</ChatConversation></Card.Content><Card.Footer><ChatPromptInput direction={direction} label={t("landing.pages.faq.label")} placeholder={t("landing.pages.faq.placeholder")} submitLabel={t("landing.pages.faq.send")} value={value} onChange={setValue} onSubmit={ask} /></Card.Footer></Card>;
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
