import { AppLayout } from "@heroui-pro/react/app-layout";

export function InvalidPage() {
  const conditionalClass = "bg-surface";

  return (
    <AppLayout>
      <main className="mx-auto min-h-screen max-w-[1200px] px-6">Content</main>
      <div className={`mx-auto max-w-[1200px] px-6 ${conditionalClass}`}>
        Template content
      </div>
      <section className="space-y-3 rounded border p-4">Bordered section</section>
      <form className={`rounded-xl ${conditionalClass} bg-surface-secondary sm:p-6`}>Background form</form>
    </AppLayout>
  );
}
