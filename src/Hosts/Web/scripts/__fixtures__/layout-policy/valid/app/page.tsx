import { Surface } from "@heroui/react";
import { StandaloneDocumentPage } from "@vitarish/ui/layout";

export function ValidPage() {
  return (
    <StandaloneDocumentPage className="flex min-w-0">
      <Surface className="flex flex-col gap-6 rounded-3xl p-6">
        <h2 className="text-lg font-semibold text-foreground">Title</h2>
        <section className="space-y-3">
          <div className="rounded-lg bg-surface-secondary p-3">Tile</div>
          <span className="vitarish-tint vitarish-context--visit bg-tint-soft text-tint-soft-foreground">Role</span>
          <span className="text-white text-danger hover:bg-default">Theme and image text</span>
        </section>
        <form className="border-t pt-4">Form</form>
      </Surface>
    </StandaloneDocumentPage>
  );
}
