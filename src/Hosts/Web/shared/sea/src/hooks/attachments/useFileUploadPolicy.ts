import { useFilesGetUploadPolicy } from "../../api/generated/files/files";

/** The server's upload policy; it changes only with a deployment, so it is read once per session. */
export function useFileUploadPolicy() {
  return useFilesGetUploadPolicy({ query: { staleTime: Infinity } });
}
