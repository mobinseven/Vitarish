"use client";

import { useMemo } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  ChatConversation,
  DashboardFrame,
  ImmersiveRegion,
} from "@vitarish/web-ui";
import { useLandingConversation } from "../conversation/use-landing-conversation";
import { ConversationContext } from "./conversation-context";
import { GuidedPage } from "./guided-page";
import { LandingComposer } from "./landing-composer";
import { LandingConversationView } from "./landing-conversation-view";
import { useLandingCopy } from "./landing-copy";

const guidedPaths = ["/about", "/portfolio", "/contact", "/explore"] as const;

export function LandingPage() {
  const { direction, t, translate } = useLandingCopy();
  const pathname = usePathname();
  const router = useRouter();
  const currentPath = pathname === "/" ? "/" : pathname;
  const conversation = useLandingConversation("home");
  const isGuidedPage = guidedPaths.includes(
    currentPath as (typeof guidedPaths)[number],
  );

  const navigationGroups = useMemo(
    () => [
      {
        label: t.workspace,
        items: [
          {
            exactMatch: true,
            href: "/",
            icon: "pen-to-square",
            label: t.newChat,
          },
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
    conversation.cancel();
    router.push(path);
  }

  async function submit(text: string) {
    if (!text.trim() || conversation.isPending) return;
    await conversation.submit(text);
  }

  return (
    <DashboardFrame
      // aside={<ConversationContext copy={t} />}
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
      // mobileAside={<ConversationContext copy={t} />}
      mobileNavigation="sidebar"
      navigate={openPath}
      navigationGroups={navigationGroups}
      navigationLabel={t.workspace}
      skipLinkLabel={t.skip}
      title={<span className="font-semibold">{t.app}</span>}
      toolbar={null}
    >
      <ImmersiveRegion className="flex min-h-0 flex-1 flex-col bg-background">
        {isGuidedPage ? (
          <ChatConversation
            className="min-h-0 flex-1"
            innerClassName="mx-auto flex w-full max-w-[714px] flex-col gap-8 px-4 pb-8 pt-8 sm:px-6"
          >
            <GuidedPage
              direction={direction}
              path={currentPath}
              t={translate}
            />
          </ChatConversation>
        ) : (
          <LandingConversationView
            copy={t}
            conversation={conversation}
            submit={submit}
          />
        )}
        {isGuidedPage ? null : (
          <LandingComposer
            key={currentPath}
            copy={t}
            conversation={conversation}
            direction={direction}
            submit={submit}
          />
        )}
      </ImmersiveRegion>
    </DashboardFrame>
  );
}
