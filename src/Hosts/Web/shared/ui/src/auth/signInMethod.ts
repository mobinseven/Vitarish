import { identityColorClassName, type IdentitySlot } from "../color/identityColor";

/**
 * A way to sign in. Each method keeps one identity color on every host so
 * users find their usual choice by color instead of rereading labels.
 */
export type SignInMethod = "password" | "phone" | "email" | "passkey";

const signInMethodSlots: Record<SignInMethod, IdentitySlot> = {
  password: 0,
  passkey: 1,
  phone: 2,
  email: 3,
};

/** Class name that paints a sign-in method's icon tile and hover tint with its color. */
export function signInMethodClassName(method: SignInMethod): string {
  return `${identityColorClassName(signInMethodSlots[method])} vitarish-sign-in-method--${method}`;
}

/** Class name for the icon tile inside an element painted by signInMethodClassName(). */
export const signInMethodTileClassName = "vitarish-sign-in-method__tile";
