import Link from "next/link";
import { Routes } from "@/app/routes";
import { getMessages } from "@/localization/messages";
import { defaultLocale } from "@/localization/locales";
import {
  PageContainer,
  PublicDocumentShell,
  StandaloneDocumentPage,
} from "@vitarish/ui/layout";

export default function HomePage() {
  const messages = getMessages(defaultLocale).landing;

  return (
    <PublicDocumentShell>
      <StandaloneDocumentPage className="flex items-center py-16">
        <PageContainer width="reading">
          <section className="w-full rounded-3xl border border-default bg-surface p-8 text-center shadow-sm sm:p-12">
            <header className="flex flex-col items-center gap-4">
              <p className="text-sm font-semibold uppercase tracking-[0.2em] text-primary">
                {messages.eyebrow}
              </p>
              <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">
                {messages.title}
              </h1>
              <p className="max-w-2xl text-base text-muted sm:text-lg">
                {messages.description}
              </p>
            </header>
            <div className="flex justify-center pt-8">
              <Link
                className="inline-flex min-h-11 items-center justify-center rounded-xl bg-primary px-6 py-3 font-semibold text-primary-foreground transition-opacity hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                href={Routes.Login}
              >
                {messages.loginAction}
              </Link>
            </div>
          </section>
        </PageContainer>
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
