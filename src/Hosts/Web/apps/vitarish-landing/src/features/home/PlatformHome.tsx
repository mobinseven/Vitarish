"use client";

import { useCallback, useEffect, useState } from "react";
import { Button } from "@heroui/react";
import { useAuthSession } from "@vitarish/auth";
import {
  AgentConversationComposer,
  InteractiveReplyMessage,
  toInteractiveReply,
  useConversationController,
  type InteractiveReply,
} from "@vitarish/intelligence";
import { formatList, useLocalizedText } from "@vitarish/localization";
import {
  landingDeleteHomeConversation,
  landingRunHomeConversation,
  useLandingListWorkspaces,
  type LandingHomeConversationResponse,
  type LandingWorkspacesResponse,
} from "@vitarish/platform";
import { ChatConversation, ChatLoadingMessage, ChatMessage, FaIcon } from "@vitarish/ui";
import { HomeWorkspaces } from "./HomeWorkspaces";
import { useHomeHandoff, type HomeHandoffTarget } from "./useHomeHandoff";

type HomeTurn =
  | { id: number; role: "person"; text: string }
  | { id: number; role: "assistant"; reply: InteractiveReply; target: HomeHandoffTarget | null };

const homeConversationOwnerKey = "platform.home";

/** The action the server validated for a reply, as something the home can do. */
function toHandoffTarget(
  response: LandingHomeConversationResponse,
  actionKeys: LandingWorkspacesResponse["actionKeys"] | undefined,
): HomeHandoffTarget | null {
  const target = response.actionTarget;
  if (!target || !actionKeys) return null;
  if (target.key === actionKeys.openWorkspace && target.tenantId) return { kind: "open", tenantId: target.tenantId };
  if (target.key === actionKeys.createWorkspace && target.appId) return { kind: "create", appId: target.appId };
  return null;
}

/** The signed-in person's platform home: a greeting with their workspaces, then a conversation (ADR-0020). */
export function PlatformHome({ signInHref }: { signInHref: string }) {
  const { t, locale } = useLocalizedText();
  const session = useAuthSession();
  const workspaces = useLandingListWorkspaces({ query: { enabled: session.isAuthenticated } });
  const handoff = useHomeHandoff();
  const [draft, setDraft] = useState("");
  const [turns, setTurns] = useState<HomeTurn[]>([]);

  useEffect(() => {
    if (!session.isLoading && !session.isAuthenticated) window.location.assign(signInHref);
  }, [session.isAuthenticated, session.isLoading, signInHref]);

  const conversation = useConversationController<LandingHomeConversationResponse>({
    ownerKey: homeConversationOwnerKey,
    isOpen: true,
    resetOnClose: false,
    run: async (message, conversationId, signal) => {
      const response = await landingRunHomeConversation({ message, conversationId }, undefined, signal);
      return { conversationId: response.conversationId, result: response };
    },
    deleteConversation: landingDeleteHomeConversation,
    isNotFound: (error) => typeof error === "object" && error !== null && "status" in error && error.status === 404,
  });

  const addReply = useCallback(
    (response: LandingHomeConversationResponse | undefined) => {
      if (!response) return;
      const reply = toInteractiveReply(response.reply ?? { text: response.message });
      setTurns((current) => [
        ...current,
        { id: current.length, role: "assistant", reply, target: toHandoffTarget(response, workspaces.data?.actionKeys) },
      ]);
    },
    [workspaces.data?.actionKeys],
  );

  const send = useCallback(
    async (value: string) => {
      const message = value.trim();
      if (!message || conversation.isPending) return;
      setDraft("");
      setTurns((current) => [...current, { id: current.length, role: "person", text: message }]);
      addReply(await conversation.submit(message));
    },
    [addReply, conversation],
  );

  if (session.isLoading || !session.isAuthenticated) {
    return (
      <main className="flex min-h-dvh items-center justify-center text-base text-muted" aria-busy="true">
        {t("home.loading")}
      </main>
    );
  }

  const givenName = session.displayProfile.givenName;
  const assistantName = t("home.assistantName");
  const assistantInitial = t("home.assistantInitial");

  return (
    <main className="flex h-dvh flex-col">
      <ChatConversation>
        <header className="flex flex-col gap-2 pt-6">
          <h1 className="text-3xl font-semibold text-foreground">
            {givenName ? t("home.greeting", { name: givenName }) : t("home.greetingAnonymous")}
          </h1>
          <p className="text-lg text-muted">{t("home.prompt")}</p>
        </header>

        <ChatMessage kind="answer" avatarAlt={assistantName} avatarFallback={assistantInitial}>
          <HomeWorkspaces
            directory={workspaces.data}
            isLoading={workspaces.isLoading}
            loadFailed={workspaces.isError}
            onRetry={() => void workspaces.refetch()}
            pending={handoff.pending}
            failed={handoff.failed}
            onStart={handoff.start}
          />
        </ChatMessage>

        {turns.map((turn) =>
          turn.role === "person" ? (
            <ChatMessage key={turn.id} kind="question">{turn.text}</ChatMessage>
          ) : (
            <InteractiveReplyMessage
              key={turn.id}
              reply={turn.reply}
              avatarAlt={assistantName}
              avatarFallback={assistantInitial}
              isPending={conversation.isPending}
              onAnswer={(keys) => {
                const labels = turn.reply.question?.options.filter((option) => keys.includes(option.key)).map((option) => option.label) ?? [];
                if (labels.length === 0 || conversation.isPending) return false;
                void send(formatList(labels, locale));
                return true;
              }}
              action={turn.target ? <HomeReplyAction target={turn.target} directory={workspaces.data} handoff={handoff} /> : undefined}
            />
          ),
        )}

        {conversation.isPending ? (
          <ChatLoadingMessage
            avatarAlt={assistantName}
            avatarFallback={assistantInitial}
            loadingLabel={t("home.thinking")}
            message={t("home.thinking")}
            title={assistantName}
          />
        ) : null}

        {conversation.isError ? (
          <div className="flex flex-wrap items-center gap-3 text-base text-danger" role="alert">
            {t("home.replyFailed")}
            <Button size="sm" variant="secondary" onPress={() => void conversation.retry().then(addReply)}>
              {t("home.retryReply")}
            </Button>
          </div>
        ) : null}
      </ChatConversation>

      <div className="mx-auto w-full max-w-[714px] shrink-0 px-4 pb-4">
        <AgentConversationComposer
          dictation={false}
          direction={locale === "fa" ? "rtl" : "ltr"}
          isPending={conversation.isPending}
          label={t("home.composer.label")}
          placeholder={t("home.composer.placeholder")}
          preferredLanguage={locale === "fa" ? "fa-IR" : "en-US"}
          sessionKey={homeConversationOwnerKey}
          submitLabel={t("home.composer.send")}
          value={draft}
          onChange={setDraft}
          onSubmit={send}
        />
      </div>
    </main>
  );
}

/** The one next step a reply offers, run through the same handoff as the workspace cards. */
function HomeReplyAction({
  target,
  directory,
  handoff,
}: {
  target: HomeHandoffTarget;
  directory: LandingWorkspacesResponse | undefined;
  handoff: ReturnType<typeof useHomeHandoff>;
}) {
  const { t } = useLocalizedText();
  const label = target.kind === "open"
    ? t("home.workspaces.openNamed", { name: directory?.workspaces.find((workspace) => workspace.tenantId === target.tenantId)?.name ?? "" })
    : t("home.workspaces.create", { app: directory?.apps.find((app) => app.appId === target.appId)?.name ?? "" });
  return (
    <Button isDisabled={handoff.pending !== null} onPress={() => handoff.start(target)}>
      {label}
      <FaIcon icon="arrow-right" aria-hidden="true" className="rtl:-scale-x-100" />
    </Button>
  );
}
