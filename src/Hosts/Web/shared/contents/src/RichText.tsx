import { createElement, Fragment, type ReactElement, type ReactNode } from "react";
import { richTextBlankTarget, richTextMarkTypes, richTextNodeTypes, richTextProperties } from "./richTextContract";

const safeNewWindowRel = "noopener noreferrer";

export interface RichTextProps {
  content: unknown;
}

export function RichText({ content }: RichTextProps): ReactElement {
  return createElement(Fragment, null, renderNode(content, "root"));
}

function renderNode(value: unknown, key: string): ReactNode {
  if (!isRecord(value) || typeof value[richTextProperties.type] !== "string") return null;

  const type = value[richTextProperties.type];
  const children = renderChildren(value[richTextProperties.content]);
  switch (type) {
    case richTextNodeTypes.document:
      return createElement(Fragment, { key }, children);
    case richTextNodeTypes.paragraph:
      return createElement("p", { key }, children);
    case richTextNodeTypes.heading: {
      const level = getHeadingLevel(value[richTextProperties.attributes]);
      if (!level) return null;
      return createElement(`h${level}`, { key }, children);
    }
    case richTextNodeTypes.bulletList:
      return createElement("ul", { key }, children);
    case richTextNodeTypes.orderedList:
      return createElement("ol", { key }, children);
    case richTextNodeTypes.listItem:
      return createElement("li", { key }, children);
    case richTextNodeTypes.blockquote:
      return createElement("blockquote", { key }, children);
    case richTextNodeTypes.hardBreak:
      return createElement("br", { key });
    case richTextNodeTypes.text: {
      const text = value[richTextProperties.text];
      return typeof text === "string" ? renderText(text, value[richTextProperties.marks], key) : null;
    }
    default:
      return null;
  }
}

function renderChildren(value: unknown): ReactNode[] {
  if (!Array.isArray(value)) return [];
  return value.map((child, index) => renderNode(child, String(index)));
}

function renderText(text: string, value: unknown, key: string): ReactNode {
  let result: ReactNode = text;
  if (!Array.isArray(value)) return result;

  value.forEach((mark, index) => {
    if (!isRecord(mark) || typeof mark[richTextProperties.type] !== "string") return;

    const type = mark[richTextProperties.type];
    switch (type) {
      case richTextMarkTypes.bold:
        result = createElement("strong", { key: `${key}-mark-${index}` }, result);
        break;
      case richTextMarkTypes.italic:
        result = createElement("em", { key: `${key}-mark-${index}` }, result);
        break;
      case richTextMarkTypes.link: {
        const rawAttributes = mark[richTextProperties.attributes];
        const attributes = isRecord(rawAttributes) ? rawAttributes : {};
        const rawHref = attributes[richTextProperties.href];
        const href = typeof rawHref === "string" ? getSafeHref(rawHref) : null;
        if (!href) break;

        const target = attributes[richTextProperties.target] === richTextBlankTarget ? richTextBlankTarget : undefined;
        result = createElement(
          "a",
          {
            key: `${key}-mark-${index}`,
            href,
            ...(target ? { target, rel: safeNewWindowRel } : {}),
          },
          result,
        );
        break;
      }
      default:
        break;
    }
  });

  return result;
}

function getHeadingLevel(attributes: unknown): 2 | 3 | 4 | null {
  if (!isRecord(attributes)) return null;
  const level = attributes[richTextProperties.level];
  return level === 2 || level === 3 || level === 4 ? level : null;
}

function getSafeHref(href: string): string | null {
  if (!href || /\s|[\\\u0000-\u001f\u007f-\u009f]/.test(href)) return null;
  if (href.startsWith("/")) return href.startsWith("//") ? null : href;

  try {
    const parsed = new URL(href);
    return parsed.protocol === "https:" && parsed.hostname && !parsed.username && !parsed.password ? href : null;
  } catch {
    return null;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
