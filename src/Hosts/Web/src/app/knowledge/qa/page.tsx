import type { Metadata } from "next";
import { KnowledgeUserQaSurface } from "@/features/knowledge/qa/components/KnowledgeUserQaSurface";

export const metadata: Metadata = {
  title: "Knowledge QA | Vitarish",
  description: "Ask grounded questions across available Knowledge corpora.",
};

export default function KnowledgeQaPage() {
  return <KnowledgeUserQaSurface />;
}
