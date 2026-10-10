/**
 * What an action does, which fixes its color everywhere: `open` views or opens
 * a record, `edit` changes or authors content, `create` adds a record, and
 * `remove` deletes one. The colors are owned by `accent.css`.
 */
export const actionRoles = ["open", "edit", "create", "remove"] as const;

export type ActionRole = (typeof actionRoles)[number];

/** Class name that paints a HeroUI Button with its action role's color. */
export function actionRoleClassName(role: ActionRole): string {
  return `vitarish-action--${role}`;
}
