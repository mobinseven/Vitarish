export type MessageCatalog = Record<string, unknown>;

export type MessageKey = string;

export type MessageParameters = Record<string, string | number | Date>;

export type Translate = (key: MessageKey, parameters?: MessageParameters) => string;
