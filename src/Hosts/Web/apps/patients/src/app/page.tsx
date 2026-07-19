import Link from "next/link";
import { Routes } from "@/app/routes";
import { PublicDocumentShell, StandaloneDocumentPage, PageContainer } from "@vitarish/web-ui/layout";

export default function PatientsHomePage() {
  return (
    <PublicDocumentShell>
      <StandaloneDocumentPage className="flex items-center py-16">
        <PageContainer width="reading">
          <section className="w-full rounded-3xl border border-default bg-surface p-8 text-center shadow-sm sm:p-12">
            <p className="text-sm font-semibold uppercase tracking-[0.2em] text-primary">Vitarish</p>
            <h1 className="mt-4 text-4xl font-bold tracking-tight sm:text-5xl">Patient portal</h1>
            <p className="mx-auto mt-4 max-w-2xl text-base text-muted sm:text-lg">Sign in to access your practice account.</p>
            <div className="flex justify-center pt-8">
              <Link className="inline-flex min-h-11 items-center justify-center rounded-xl bg-primary px-6 py-3 font-semibold text-primary-foreground" href={Routes.Login.Index}>Sign in</Link>
            </div>
          </section>
        </PageContainer>
      </StandaloneDocumentPage>
    </PublicDocumentShell>
  );
}
