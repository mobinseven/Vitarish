import { readSignInUrl } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";
import { OwnerAccessGuard } from "@/components/auth/OwnerAccessGuard";

// The sign-in address comes from the running server's environment, not the build.
export const dynamic = "force-dynamic";

export default function OwnerLayout({ children }: { children: React.ReactNode }) {
  // The sign-in host's continue page takes a signed-in person to their platform home, which lists their workspaces.
  const signInUrl = readSignInUrl(process.env);
  const workspacesHref = signInUrl ? new URL(Routes.SignInContinue, signInUrl).toString() : null;
  return <OwnerAccessGuard workspacesHref={workspacesHref}>{children}</OwnerAccessGuard>;
}
