export const richTextNodeTypes = {
  document: "doc",
  paragraph: "paragraph",
  heading: "heading",
  bulletList: "bulletList",
  orderedList: "orderedList",
  listItem: "listItem",
  blockquote: "blockquote",
  hardBreak: "hardBreak",
  text: "text",
} as const;

export const richTextMarkTypes = {
  bold: "bold",
  italic: "italic",
  link: "link",
} as const;

export const richTextProperties = {
  type: "type",
  content: "content",
  attributes: "attrs",
  text: "text",
  marks: "marks",
  level: "level",
  href: "href",
  target: "target",
} as const;

export const richTextBlankTarget = "_blank";
