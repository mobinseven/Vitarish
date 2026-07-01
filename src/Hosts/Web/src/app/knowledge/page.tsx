import type { Metadata } from "next";
import { KnowledgeUserQaSurface } from "@/features/knowledge/qa/components/KnowledgeUserQaSurface";
import { KnowledgePageWrapper } from "./KnowledgePageWrapper";

export const metadata: Metadata = {
  title: "Knowledge | Vitarish",
  description: "Ask grounded questions across available Knowledge corpora.",
};

export default function KnowledgePage() {
  return (
    <KnowledgePageWrapper>
      <KnowledgeUserQaSurface />
    </KnowledgePageWrapper>
  );
}
