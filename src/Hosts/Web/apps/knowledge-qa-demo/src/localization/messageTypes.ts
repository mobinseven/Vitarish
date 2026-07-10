import type { MessageCatalog } from "./messages";

type Join<K, P> = K extends string | number ? P extends string | number ? `${K}.${P}` : never : never;
type Leaves<T> = T extends string ? never : { [K in keyof T]: T[K] extends string ? K : T[K] extends Record<string, unknown> ? Join<K, Leaves<T[K]>> : never }[keyof T];

export type MessageKey = Leaves<MessageCatalog> & string;
export type MessageParameters = Record<string, string | number | Date>;
export type Translate = (key: MessageKey, parameters?: MessageParameters) => string;
