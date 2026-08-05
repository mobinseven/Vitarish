import { useLocalizedText } from "@vitarish/web-localization";

export type LandingCopy = {
  app: string;
  subtitle: string;
  workspace: string;
  recent: string;
  newChat: string;
  about: string;
  portfolio: string;
  contact: string;
  explore: string;
  chatTitle: string;
  intro: string;
  promptLabel: string;
  placeholder: string;
  send: string;
  ready: string;
  thinking: string;
  thinkingBody: string;
  context: string;
  contextBody: string;
  contextStatus: string;
  privateWorkspace: string;
  disclaimer: string;
  skip: string;
  suggestionTitle: string;
  suggestionDescription: string;
  suggestions: { icon: string; text: string }[];
  error: string;
  retry: string;
  reset: string;
  retryTooltip: string;
  resetTooltip: string;
};

export function useLandingCopy() {
  const { direction, t: translate } = useLocalizedText();
  const t: LandingCopy = {
    app: translate("landing.app"),
    subtitle: translate("landing.subtitle"),
    workspace: translate("landing.workspace"),
    recent: translate("landing.recent"),
    newChat: translate("landing.newChat"),
    about: translate("landing.about"),
    portfolio: translate("landing.portfolio"),
    contact: translate("landing.contact"),
    explore: translate("landing.explore"),
    chatTitle: translate("landing.chatTitle"),
    intro: translate("landing.intro"),
    promptLabel: translate("landing.promptLabel"),
    placeholder: translate("landing.placeholder"),
    send: translate("landing.send"),
    ready: translate("landing.ready"),
    thinking: translate("landing.thinking"),
    thinkingBody: translate("landing.thinkingBody"),
    context: translate("landing.context"),
    contextBody: translate("landing.contextBody"),
    contextStatus: translate("landing.contextStatus"),
    privateWorkspace: translate("landing.privateWorkspace"),
    disclaimer: translate("landing.disclaimer"),
    skip: translate("landing.skip"),
    suggestionTitle: translate("landing.suggestionTitle"),
    suggestionDescription: translate("landing.suggestionDescription"),
    suggestions: [
      { icon: "compass", text: translate("landing.suggestion0") },
      { icon: "lightbulb", text: translate("landing.suggestion1") },
      { icon: "list-check", text: translate("landing.suggestion2") },
      { icon: "pen-to-square", text: translate("landing.suggestion3") },
    ],
    error: translate("landing.error"),
    retry: translate("landing.retry"),
    reset: translate("landing.reset"),
    retryTooltip: translate("landing.retryTooltip"),
    resetTooltip: translate("landing.resetTooltip"),
  };

  return { direction, t, translate };
}
