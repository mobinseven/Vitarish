"use client";

import { ComboBox, Input, Label, ListBox, Switch } from "@heroui/react";
import { EditorContent, useEditor, type JSONContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import { useLocalizedText } from "@vitarish/localization";
import { removeButtonClassName } from "@vitarish/ui";
import { useState } from "react";
import { richTextBlankTarget, richTextMarkTypes, richTextNodeTypes } from "../richTextContract";
import { contentFieldKind, type ContentFieldDefinition } from "./contentsStudioApi";
import { StudioButton, StudioFailure, StudioInput, StudioTextArea } from "./StudioControls";
import { useContentEntries } from "./useContentsStudio";

// The entries endpoint accepts at most 100 per page; the picker offers the first page of the site.
const referenceCandidateLimit = 100;

const objectValue = (value: unknown): Record<string, unknown> => value !== null && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {};
const stringValue = (value: unknown) => typeof value === "string" ? value : "";
export function updateFieldValue(values: Record<string, unknown>, key: string, value: unknown): Record<string, unknown> {
  const next = { ...values };
  if (value === undefined) delete next[key];
  else next[key] = value;
  return next;
}

// Tiptap adds presentation attributes which are not part of the delivery contract.
function contractDocument(node: JSONContent): JSONContent {
  const result: JSONContent = { type: node.type };
  if (node.text !== undefined) result.text = node.text;
  if (node.type === richTextNodeTypes.heading) result.attrs = { level: node.attrs?.level };
  if (node.content) result.content = node.content.map(contractDocument);
  if (node.marks) result.marks = node.marks.map((mark) => mark.type === richTextMarkTypes.link ? { type: mark.type, attrs: { href: mark.attrs?.href, ...(mark.attrs?.target === richTextBlankTarget ? { target: richTextBlankTarget } : {}) } } : { type: mark.type });
  return result;
}

function RichTextInput({ label, value, onChange }: { label: string; value: unknown; onChange: (value: unknown) => void }) {
  const { t } = useLocalizedText();
  const [href, setHref] = useState("");
  const editor = useEditor({
    immediatelyRender: false,
    extensions: [StarterKit.configure({ heading: { levels: [2, 3, 4] }, code: false, codeBlock: false, horizontalRule: false, strike: false, underline: false, link: { openOnClick: false } })],
    content: value && typeof value === "object" ? value as JSONContent : { type: richTextNodeTypes.document, content: [{ type: richTextNodeTypes.paragraph }] },
    editorProps: { attributes: { "aria-label": label, class: "min-h-40 rounded-2xl border border-default p-4 text-base focus:outline-none focus-visible:ring-2 focus-visible:ring-focus" } },
    onUpdate: ({ editor: current }) => onChange(contractDocument(current.getJSON())),
  });
  if (!editor) return null;
  return <div className="flex flex-col gap-3"><span className="text-base font-medium">{label}</span><div className="flex flex-wrap gap-2" role="toolbar" aria-label={label}>
    <StudioButton label={t("contentsStudio.bold")} icon="fa-bold" variant="tertiary" onPress={() => { editor.chain().focus().toggleBold().run(); }} />
    <StudioButton label={t("contentsStudio.italic")} icon="fa-italic" variant="tertiary" onPress={() => { editor.chain().focus().toggleItalic().run(); }} />
    <StudioButton label={t("contentsStudio.paragraph")} icon="fa-paragraph" variant="tertiary" onPress={() => { editor.chain().focus().setParagraph().run(); }} />
    {([2, 3, 4] as const).map((level) => <StudioButton key={level} label={`${t("contentsStudio.heading")} ${level}`} icon={`fa-h${level}`} variant="tertiary" onPress={() => { editor.chain().focus().toggleHeading({ level }).run(); }} />)}
    <StudioButton label={t("contentsStudio.bulletList")} icon="fa-list-ul" variant="tertiary" onPress={() => { editor.chain().focus().toggleBulletList().run(); }} />
    <StudioButton label={t("contentsStudio.orderedList")} icon="fa-list-ol" variant="tertiary" onPress={() => { editor.chain().focus().toggleOrderedList().run(); }} />
    <StudioButton label={t("contentsStudio.quote")} icon="fa-quote-left" variant="tertiary" onPress={() => { editor.chain().focus().toggleBlockquote().run(); }} />
  </div><div className="flex flex-wrap items-end gap-2"><div className="min-w-60 flex-1"><StudioInput label={t("contentsStudio.href")} value={href} onChange={setHref} /></div><StudioButton label={t("contentsStudio.link")} icon="fa-link" variant="tertiary" onPress={() => { editor.chain().focus().extendMarkRange(richTextMarkTypes.link).setLink({ href }).run(); }} /></div><EditorContent editor={editor} /></div>;
}

/** Picks an entry of the same site by title; an id outside the loaded candidates stays visible as-is. */
function ReferencePicker({ label, siteId, value, onChange }: { label: string; siteId: string; value: string; onChange: (value: string) => void }) {
  const { t } = useLocalizedText();
  const candidates = useContentEntries({ siteId, take: referenceCandidateLimit });
  const items = [
    ...(value && !candidates.data?.some((entry) => entry.id === value) ? [{ id: value, title: value }] : []),
    ...(candidates.data ?? []).map((entry) => ({ id: entry.id, title: entry.title })),
  ];
  return <div className="flex flex-col gap-1">
    <ComboBox variant="secondary" items={items} selectedKey={value || null} onSelectionChange={(key) => onChange(key === null ? "" : String(key))}>
      <Label className="text-base">{label}</Label>
      <ComboBox.InputGroup><Input className="text-base" placeholder={t("contentsStudio.reference")} /><ComboBox.Trigger /></ComboBox.InputGroup>
      <ComboBox.Popover><ListBox>{(item: { id: string; title: string }) => <ListBox.Item className="text-base" id={item.id} textValue={item.title}><bdi dir="auto">{item.title}</bdi><ListBox.ItemIndicator /></ListBox.Item>}</ListBox></ComboBox.Popover>
    </ComboBox>
    {candidates.isError && <StudioFailure error={candidates.error} />}
  </div>;
}

export function ContentFieldEditor({ definition, value, onChange, locale, siteId, path, errors }: { definition: ContentFieldDefinition; value: unknown; onChange: (value: unknown) => void; locale: string; siteId: string; path: string; errors: Record<string, string[]> }) {
  const { t } = useLocalizedText();
  const [reset, setReset] = useState(0);
  const actual = definition.localized ? objectValue(value)[locale] : value;
  const set = (next: unknown) => {
    if (!definition.localized) return onChange(next);
    const localized = updateFieldValue(objectValue(value), locale, next);
    onChange(Object.keys(localized).length ? localized : undefined);
  };
  const fieldPath = definition.localized ? `${path}.${locale}` : path;
  const error = errors[fieldPath]?.join(" ") ?? errors[path]?.join(" ");
  const common = { locale, siteId, errors };
  let input;
  switch (definition.kind) {
    case contentFieldKind.longText: input = <StudioTextArea label={definition.label} value={stringValue(actual)} onChange={set} error={error} />; break;
    case contentFieldKind.richText: input = <RichTextInput key={`${fieldPath}-${reset}`} label={definition.label} value={actual} onChange={set} />; break;
    case contentFieldKind.boolean: input = <Switch isSelected={actual === true} onChange={set}><Label>{definition.label}</Label><Switch.Control><Switch.Thumb /></Switch.Control></Switch>; break;
    case contentFieldKind.number: input = <StudioInput label={definition.label} type="number" value={typeof actual === "number" ? String(actual) : ""} onChange={(next) => set(next === "" ? null : Number(next))} error={error} />; break;
    case contentFieldKind.date: input = <StudioInput label={definition.label} type="date" value={stringValue(actual)} onChange={set} error={error} />; break;
    case contentFieldKind.reference: {
      input = <ReferencePicker label={definition.label} siteId={siteId} value={stringValue(actual)} onChange={(next) => set(next === "" ? undefined : next)} />; break;
    }
    case contentFieldKind.link: case contentFieldKind.media: {
      const record = objectValue(actual);
      const keys = definition.kind === contentFieldKind.link ? ["href", "label"] : ["url", "alt"];
      input = <div className="grid gap-3 md:grid-cols-2"><span className="text-base font-medium md:col-span-2">{definition.label}</span>{keys.map((key) => <StudioInput key={key} label={t(`contentsStudio.${key === "label" ? "linkLabel" : key}`)} value={stringValue(record[key])} onChange={(next) => set({ ...record, [key]: next })} error={errors[`${fieldPath}.${key}`]?.join(" ")} />)}</div>; break;
    }
    case contentFieldKind.group: input = <fieldset className="flex flex-col gap-4 rounded-2xl border border-default p-4"><legend className="px-1 text-base font-semibold">{definition.label}</legend>{(definition.fields ?? []).map((child) => <ContentFieldEditor key={child.key} {...common} definition={child} value={objectValue(actual)[child.key]} path={`${fieldPath}.${child.key}`} onChange={(next) => set(updateFieldValue(objectValue(actual), child.key, next))} />)}</fieldset>; break;
    case contentFieldKind.list: {
      const items = Array.isArray(actual) ? actual : [];
      const removeItem = (index: number) => { set(items.filter((_, i) => i !== index)); setReset((n) => n + 1); };
      input = <fieldset className="flex flex-col gap-4 rounded-2xl border border-default p-4"><legend className="px-1 text-base font-semibold">{definition.label}</legend>{definition.item && items.map((item, index) => <div key={`${reset}-${index}`} className="flex flex-col gap-2"><ContentFieldEditor {...common} definition={definition.item!} value={item} path={`${fieldPath}[${index}]`} onChange={(next) => next === undefined ? removeItem(index) : set(items.map((old, i) => i === index ? next : old))} /><StudioButton label={t("contentsStudio.remove")} icon="fa-trash" className={removeButtonClassName} onPress={() => removeItem(index)} /></div>)}<StudioButton label={t("contentsStudio.add")} icon="fa-plus" onPress={() => set([...items, null])} /></fieldset>; break;
    }
    default: input = <StudioInput label={definition.label} value={stringValue(actual)} onChange={set} error={error} />;
  }
  return <div className="flex flex-col gap-2">{input}{actual !== undefined && <StudioButton label={`${t("contentsStudio.remove")} ${definition.label}`} icon="fa-eraser" variant="ghost" onPress={() => { set(undefined); setReset((n) => n + 1); }} />}{error && ([contentFieldKind.richText, contentFieldKind.boolean, contentFieldKind.reference, contentFieldKind.list, contentFieldKind.group] as readonly string[]).includes(definition.kind) && <p className="text-base text-danger" role="alert">{error}</p>}</div>;
}
