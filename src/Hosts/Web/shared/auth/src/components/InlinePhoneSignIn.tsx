"use client";

import { Surface } from "@heroui/react";
import { PhoneOtpForm } from "./PhoneOtpForm";

export interface InlinePhoneSignInProps {
  /** Why the person is asked to sign in here, in the conversation's own words. */
  title: string;
  description?: string;
  /** Called once the code is accepted and the session exists; the page stays where it is. */
  onSignedIn: () => void;
}

/**
 * Phone sign-in that lives inside a conversation instead of on a sign-in page: phone number,
 * then code, then the conversation continues. Signing in with a new number also creates the
 * account. The server decides whether this host admits the session.
 */
export function InlinePhoneSignIn({ title, description, onSignedIn }: InlinePhoneSignInProps) {
  return (
    <Surface className="flex flex-col gap-4 p-4" variant="secondary">
      <div className="flex flex-col gap-1">
        <p className="font-semibold text-foreground">{title}</p>
        {description ? <p className="text-muted">{description}</p> : null}
      </div>
      <PhoneOtpForm navigate={() => onSignedIn()} />
    </Surface>
  );
}
