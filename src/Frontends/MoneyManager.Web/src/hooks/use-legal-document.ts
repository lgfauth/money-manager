import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export interface LegalDocumentDto {
  slug: string;
  title: string;
  content: string;
  version: string;
  lastUpdatedAt: string;
}

export function useLegalDocument(slug: "termos" | "privacidade") {
  return useQuery({
    queryKey: ["legal-document", slug],
    queryFn: () => apiClient.get<LegalDocumentDto>(`/api/documents/${slug}`),
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });
}
