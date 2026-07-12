import type { Metadata } from "next";
import { KnowledgeQaPageSurface } from "../KnowledgeQaPageSurface";

export const metadata: Metadata = {
  title: "Knowledge QA | Vitarish",
  description: "Ask grounded questions across available Knowledge corpora.",
};

export default function KnowledgeQaPage() {
  return <KnowledgeQaPageSurface />;
}
