import { createKnowledgeQaClient, getKnowledgeQaErrorMessage } from "@vitarish/knowledge";
import { apiFetch } from "@/api/client/apiFetch";

export const knowledgeQaClient = createKnowledgeQaClient(apiFetch as <T>(url: string, options?: RequestInit) => Promise<T>);

export { getKnowledgeQaErrorMessage };
