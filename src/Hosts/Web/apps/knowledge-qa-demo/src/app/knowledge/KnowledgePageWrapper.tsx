"use client";

export function KnowledgePageWrapper({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex items-center border-b px-4 py-3">
        <h1 className="text-lg font-semibold">Knowledge QA Demo</h1>
      </header>
      <main className="flex-1">{children}</main>
    </div>
  );
}
