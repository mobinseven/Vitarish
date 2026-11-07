"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Alert, Button, Tooltip } from "@heroui/react";
import { useRouter } from "next/navigation";
import { useAuthErrorDisplay, useAuthSession, useLogout } from "@vitarish/auth";
import {
  AgentConversationComposer,
  InteractiveReplyMessage,
  toInteractiveReply,
  useConversationController,
  type InteractiveReply,
} from "@vitarish/intelligence";
import { formatList, useLocalizedText } from "@vitarish/localization";
import {
  ApiError,
  landingDeleteHomeConversation,
  landingRunHomeConversation,
  useLandingListWorkspaces,
  type LandingHomeConversationResponse,
  type LandingWorkspacesResponse,
} from "@vitarish/platform";
import { ChatConversation, ChatLoadingMessage, ChatMessage, FaIcon } from "@vitarish/ui";
import { LandingRoutes } from "../../app/routes";
import { HomeWorkspaces } from "./HomeWorkspaces";
import { useHomeHandoff, type HomeHandoffTarget } from "./useHomeHandoff";

type HomeTurn =
  | { id: number; role: "person"; text: string }
  | { id: number; role: "assistant"; reply: InteractiveReply; target: HomeHandoffTarget | null };

const homeConversationOwnerKey = "platform.home";
const homeConversationMessageLimit = 2_000;

interface HomeSubmission {
  draft: string;
  message: string;
}

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
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);
  const session = useAuthSession();
  const {
    logout,
    isLoading: isSigningOut,
    isContextLoading: isLogoutContextLoading,
    error: signOutError,
    errorMessage: signOutErrorMessage,
  } = useLogout(
    undefined,
    LandingRoutes.Home,
    navigate,
  );
  const signOutFailure = useAuthErrorDisplay(signOutError, signOutErrorMessage);
  const workspaces = useLandingListWorkspaces({ query: { enabled: session.isAuthenticated } });
  const handoff = useHomeHandoff();
  const [draft, setDraft] = useState("");
  const [turns, setTurns] = useState<HomeTurn[]>([]);
  const [requiresNewConversation, setRequiresNewConversation] = useState(false);
  const lastSubmission = useRef<HomeSubmission | null>(null);

  useEffect(() => {
    if (!session.isLoading && !session.isError && !session.isAuthenticated) window.location.assign(signInHref);
  }, [session.isAuthenticated, session.isError, session.isLoading, signInHref]);

  const conversation = useConversationController<LandingHomeConversationResponse>({
    ownerKey: homeConversationOwnerKey,
    isOpen: true,
    resetOnClose: false,
    run: async (message, conversationId, signal) => {
      try {
        const response = await landingRunHomeConversation({ message, conversationId }, undefined, signal);
        return { conversationId: response.conversationId, result: response };
      } catch (error) {
        if (error instanceof ApiError && error.status === 409) setRequiresNewConversation(true);
        throw error;
      }
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
      lastSubmission.current = { draft: value, message };
      const response = await conversation.submit(message);
      if (!response) return;
      setTurns((current) => [...current, { id: current.length, role: "person", text: message }]);
      addReply(response);
      setDraft((current) => current === value ? "" : current);
      setRequiresNewConversation(false);
    },
    [addReply, conversation],
  );

  const retryConversation = useCallback(async () => {
    const submission = lastSubmission.current;
    const response = await conversation.retry();
    if (!response || !submission) return;
    setTurns((current) => [...current, { id: current.length, role: "person", text: submission.message }]);
    addReply(response);
    setDraft((current) => current === submission.draft ? "" : current);
    setRequiresNewConversation(false);
  }, [addReply, conversation]);

  const startNewConversation = useCallback(() => {
    conversation.reset();
    lastSubmission.current = null;
    setTurns([]);
    setRequiresNewConversation(false);
  }, [conversation]);

  if (session.isError) {
    return (
      <main className="flex min-h-dvh items-center justify-center px-4">
        <Alert className="w-full max-w-xl" status="danger">
          <Alert.Indicator><FaIcon icon="triangle-exclamation" aria-hidden="true" /></Alert.Indicator>
          <Alert.Content>
            <Alert.Title>{t("home.sessionUnavailable")}</Alert.Title>
            <Alert.Description>
              <Tooltip>
                <Button variant="secondary" onPress={() => void session.refetch()}>
                  <FaIcon icon="rotate-right" aria-hidden="true" />
                  {t("home.retrySession")}
                </Button>
                <Tooltip.Content>{t("home.retrySession")}</Tooltip.Content>
              </Tooltip>
            </Alert.Description>
          </Alert.Content>
        </Alert>
      </main>
    );
  }

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
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex min-w-0 flex-col gap-2">
              <h1 className="text-2xl font-semibold text-foreground sm:text-3xl">
                {givenName ? <>{t("home.greetingBeforeName")}<bdi>{givenName}</bdi>{t("home.greetingAfterName")}</> : t("home.greetingAnonymous")}
              </h1>
              <p className="text-lg text-muted">{t("home.prompt")}</p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Tooltip>
                <Button variant="tertiary" onPress={() => navigate(LandingRoutes.Account)}>
                  <FaIcon icon="user-gear" aria-hidden="true" />
                  {t("home.account")}
                </Button>
                <Tooltip.Content>{t("home.account")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  variant="tertiary"
                  isDisabled={isSigningOut || isLogoutContextLoading}
                  isPending={isSigningOut}
                  onPress={() => void logout(LandingRoutes.Home)}
                >
                  <FaIcon icon="arrow-right-from-bracket" aria-hidden="true" className="rtl:-scale-x-100" />
                  {t("home.signOut")}
                </Button>
                <Tooltip.Content>{t("home.signOut")}</Tooltip.Content>
              </Tooltip>
            </div>
          </div>
          {signOutFailure ? (
            <Alert status="danger">
              <Alert.Indicator><FaIcon icon="triangle-exclamation" aria-hidden="true" /></Alert.Indicator>
              <Alert.Content><Alert.Description>{signOutFailure}</Alert.Description></Alert.Content>
            </Alert>
          ) : null}
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
          <Alert className="flex flex-wrap items-center gap-3" status="danger">
            <Alert.Indicator><FaIcon icon="triangle-exclamation" aria-hidden="true" /></Alert.Indicator>
            <Alert.Content><Alert.Title>{t("home.replyFailed")}</Alert.Title></Alert.Content>
            <Tooltip>
              <Button size="sm" variant="secondary" onPress={() => void retryConversation()}>
                <FaIcon icon="rotate-right" aria-hidden="true" />
                {t("home.retryReply")}
              </Button>
              <Tooltip.Content>{t("home.retryReply")}</Tooltip.Content>
            </Tooltip>
            {requiresNewConversation ? (
              <Tooltip>
                <Button size="sm" variant="secondary" onPress={startNewConversation}>
                  <FaIcon icon="arrow-rotate-right" aria-hidden="true" />
                  {t("home.startNewConversation")}
                </Button>
                <Tooltip.Content>{t("home.startNewConversation")}</Tooltip.Content>
              </Tooltip>
            ) : null}
          </Alert>
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
          onChange={(value) => setDraft(value.slice(0, homeConversationMessageLimit))}
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
  const workspaceName = target.kind === "open"
    ? directory?.workspaces.find((workspace) => workspace.tenantId === target.tenantId)?.name ?? ""
    : "";
  const appName = target.kind === "create"
    ? directory?.apps.find((app) => app.appId === target.appId)?.name ?? ""
    : "";
  return (
    <Tooltip>
      <Button
        className="max-w-full whitespace-normal"
        aria-label={target.kind === "open" ? t("home.workspaces.openNamed", { name: workspaceName }) : undefined}
        isDisabled={handoff.pending !== null}
        onPress={() => handoff.start(target)}
      >
        <span className="min-w-0">
          {target.kind === "open" ? <>{t("home.workspaces.openNamedBeforeName")}<bdi>{workspaceName}</bdi>{t("home.workspaces.openNamedAfterName")}</> : <>{t("home.workspaces.createBeforeApp")}<bdi>{appName}</bdi>{t("home.workspaces.createAfterApp")}</>}
        </span>
        <FaIcon icon="arrow-right" aria-hidden="true" className="rtl:-scale-x-100" />
      </Button>
      <Tooltip.Content>
        {target.kind === "open" ? <>{t("home.workspaces.openNamedBeforeName")}<bdi>{workspaceName}</bdi>{t("home.workspaces.openNamedAfterName")}</> : <>{t("home.workspaces.createBeforeApp")}<bdi>{appName}</bdi>{t("home.workspaces.createAfterApp")}</>}
      </Tooltip.Content>
    </Tooltip>
  );
}
