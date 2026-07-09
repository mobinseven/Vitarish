import type { Metadata } from "next";
import { KnowledgeQaPageSurface } from "./KnowledgeQaPageSurface";
import { KnowledgePageWrapper } from "./KnowledgePageWrapper";

export const metadata: Metadata = {
  title: "Knowledge | Vitarish",
  description: "Ask grounded questions across available Knowledge corpora.",
};

export default function KnowledgePage() {
  return (
    <KnowledgePageWrapper>
      <KnowledgeQaPageSurface />
    </KnowledgePageWrapper>
  );
}
