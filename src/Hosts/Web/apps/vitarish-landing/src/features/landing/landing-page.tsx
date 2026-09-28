"use client";

import { useMemo, useState, type Key } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Button, Popover, Tabs, Tooltip } from "@heroui/react";
import { AgentConversationComposer } from "@vitarish/intelligence";
import {
  DashboardFrame,
  ImmersiveRegion,
  FaIcon,
} from "@vitarish/ui";
import { useLandingConversation } from "../conversation/use-landing-conversation";
import { ConversationContext } from "./conversation-context";
import { GuidedPage } from "./guided-page";
import { LandingComposer } from "./landing-composer";
import { LandingConversationView } from "./landing-conversation-view";
import { useLandingCopy } from "./landing-copy";
import { GuidedRoutes, Routes } from "../../app/routes";

export function LandingPage() {
  const { direction, t, translate } = useLandingCopy();
  const pathname = usePathname();
  const router = useRouter();
  const currentPath = pathname === Routes.Home ? Routes.Home : pathname;
  const conversation = useLandingConversation("home");
  const isGuidedPage = GuidedRoutes.includes(currentPath as (typeof GuidedRoutes)[number]);

  const navigationGroups = useMemo(
    () => [
      {
        label: t.workspace,
        items: [
          {
            exactMatch: true,
            href: Routes.Home,
            icon: "pen-to-square",
            label: t.newChat,
          },
          { href: Routes.About, icon: "heart", label: t.about },
          { href: Routes.Explore, icon: "compass", label: t.explore },
        ],
      },
      {
        label: t.recent,
        items: [
          { href: Routes.Portfolio, icon: "images", label: t.portfolio },
          { href: Routes.Contact, icon: "envelope", label: t.contact },
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

  if (currentPath === Routes.Home && conversation.messages.length === 0) {
    return <LandingInitialHome copy={t} direction={direction} submit={submit} />;
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
        {isGuidedPage ? (
          <div className="min-h-0 flex-1 overflow-y-auto">
            <GuidedPage
              direction={direction}
              path={currentPath}
              t={translate}
            />
          </div>
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

type LandingMode = "chat" | "work";

function LandingInitialHome({
  copy,
  direction,
  submit,
}: {
  copy: ReturnType<typeof useLandingCopy>["t"];
  direction: "ltr" | "rtl";
  submit: (text: string) => Promise<void>;
}) {
  const [value, setValue] = useState("");
  const [mode, setMode] = useState<LandingMode>("work");
  const quickStarts = copy.suggestions.slice(0, 3);

  return (
    <main id="vitarish-landing-main" className="landing-work-home" aria-label={copy.workspace} dir={direction}>
      <a className="vitarish-skip-link" href="#vitarish-landing-main">{copy.skip}</a>
      <aside className="landing-work-rail" aria-label={copy.workspace}>
        <a className="landing-rail-mark" href="/" aria-label={copy.app}>
          <img src="/512.webp" alt="" />
        </a>
        <nav className="landing-rail-nav" aria-label={copy.workspace}>
          <a href="/" aria-label={copy.newChat} title={copy.newChat}><i className="fa-light fa-pen-to-square" aria-hidden="true" /></a>
          <a href="/explore" aria-label={copy.explore} title={copy.explore}><i className="fa-light fa-magnifying-glass" aria-hidden="true" /></a>
          <a href="/portfolio" aria-label={copy.portfolio} title={copy.portfolio}><i className="fa-light fa-thumbtack" aria-hidden="true" /></a>
          <a href="/contact" aria-label={copy.contact} title={copy.contact}><i className="fa-light fa-message" aria-hidden="true" /></a>
        </nav>
        <span className="landing-rail-avatar" role="img" aria-label={copy.app}>V</span>
      </aside>

      <section className="landing-work-stage">
        <Tabs
          className="landing-mode-switch"
          selectedKey={mode}
          onSelectionChange={(key: Key) => setMode(String(key) as LandingMode)}
        >
          <Tabs.ListContainer>
            <Tabs.List aria-label={copy.modeLabel}>
              <Tabs.Tab id="chat">
                {copy.chatMode}
                <Tabs.Indicator />
              </Tabs.Tab>
              <Tabs.Tab id="work">
                {copy.workMode}
                <Tabs.Indicator />
              </Tabs.Tab>
            </Tabs.List>
          </Tabs.ListContainer>
        </Tabs>

        <div className="landing-work-center">
          <h1>{mode === "work" ? copy.workHomeTitle : copy.chatHomeTitle}</h1>
          <AgentConversationComposer
            direction={direction}
            label={copy.promptLabel}
            placeholder={mode === "work" ? copy.workHomePlaceholder : copy.chatHomePlaceholder}
            submitLabel={copy.send}
            preferredLanguage={direction === "rtl" ? "fa-IR" : "en-US"}
            sessionKey="landing-home"
            value={value}
            onChange={setValue}
            onSubmit={async (text) => {
              setValue("");
              await submit(text);
            }}
            toolbarStart={
              <PluginPicker
                copy={copy}
                iconOnly
              />
            }
          />
          <div className="landing-work-links">
            <EmptyResourcePopover
              icon="folder"
              label={copy.chooseProject}
              title={copy.projectsEmpty}
              description={copy.projectsEmptyDescription}
            />
            <PluginPicker copy={copy} />
          </div>
          <div className="landing-quick-starts">
            {quickStarts.map((item) => (
              <Button
                key={item.text}
                className="landing-quick-start"
                size="sm"
                variant="ghost"
                onPress={() => void submit(item.text)}
              >
                <FaIcon aria-hidden="true" icon={item.icon} />
                <span>{item.text}</span>
              </Button>
            ))}
          </div>
        </div>
      </section>
    </main>
  );
}

function PluginPicker({
  copy,
  iconOnly = false,
}: {
  copy: ReturnType<typeof useLandingCopy>["t"];
  iconOnly?: boolean;
}) {
  return (
    <Popover>
      <Tooltip>
        <Button
          aria-label={copy.plugins}
          className={iconOnly ? "landing-prompt-plugin" : "landing-work-link"}
          isIconOnly={iconOnly}
          size="sm"
          variant="ghost"
        >
          <FaIcon aria-hidden="true" icon={iconOnly ? "plus" : "puzzle-piece"} />
          {iconOnly ? null : copy.plugins}
        </Button>
        <Tooltip.Content>{copy.plugins}</Tooltip.Content>
      </Tooltip>
      <Popover.Content className="landing-empty-popover">
        <Popover.Dialog>
          <Popover.Heading>{copy.pluginsEmpty}</Popover.Heading>
          <p>{copy.pluginsEmptyDescription}</p>
        </Popover.Dialog>
      </Popover.Content>
    </Popover>
  );
}

function EmptyResourcePopover({
  description,
  icon,
  label,
  title,
}: {
  description: string;
  icon: string;
  label: string;
  title: string;
}) {
  return (
    <Popover>
      <Button
        aria-label={label}
        className="landing-work-link"
        size="sm"
        variant="ghost"
      >
        <FaIcon aria-hidden="true" icon={icon} />
        {label}
      </Button>
      <Popover.Content className="landing-empty-popover">
        <Popover.Dialog>
          <Popover.Heading>{title}</Popover.Heading>
          <p>{description}</p>
        </Popover.Dialog>
      </Popover.Content>
    </Popover>
  );
}
