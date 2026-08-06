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
  chatMode: string;
  workMode: string;
  modeLabel: string;
  chatHomeTitle: string;
  workHomeTitle: string;
  chatHomePlaceholder: string;
  workHomePlaceholder: string;
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
  chooseProject: string;
  projectsEmpty: string;
  projectsEmptyDescription: string;
  plugins: string;
  pluginsEmpty: string;
  pluginsEmptyDescription: string;
  modelSelector: string;
  modelFast: string;
  modelBalanced: string;
  modelReasoning: string;
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
    chatMode: translate("landing.chatMode"),
    workMode: translate("landing.workMode"),
    modeLabel: translate("landing.modeLabel"),
    chatHomeTitle: translate("landing.chatHomeTitle"),
    workHomeTitle: translate("landing.workHomeTitle"),
    chatHomePlaceholder: translate("landing.chatHomePlaceholder"),
    workHomePlaceholder: translate("landing.workHomePlaceholder"),
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
    chooseProject: translate("landing.chooseProject"),
    projectsEmpty: translate("landing.projectsEmpty"),
    projectsEmptyDescription: translate("landing.projectsEmptyDescription"),
    plugins: translate("landing.plugins"),
    pluginsEmpty: translate("landing.pluginsEmpty"),
    pluginsEmptyDescription: translate("landing.pluginsEmptyDescription"),
    modelSelector: translate("landing.modelSelector"),
    modelFast: translate("landing.modelFast"),
    modelBalanced: translate("landing.modelBalanced"),
    modelReasoning: translate("landing.modelReasoning"),
  };

  return { direction, t, translate };
}
