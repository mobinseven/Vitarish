/**
 * A way to sign in. Each method keeps one color on every host so users find
 * their usual choice by color instead of rereading labels. The colors are
 * owned by `accent.css`.
 */
export type SignInMethod = "password" | "phone" | "email" | "passkey";

/** Class name that paints a sign-in method's icon tile and hover tint with its color. */
export function signInMethodClassName(method: SignInMethod): string {
  return `vitarish-sign-in-method--${method}`;
}

/** Class name for the icon tile inside an element painted by signInMethodClassName(). */
export const signInMethodTileClassName = "vitarish-sign-in-method__tile";
