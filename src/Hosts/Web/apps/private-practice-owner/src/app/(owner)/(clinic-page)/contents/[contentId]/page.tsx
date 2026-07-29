import { ContentsPageSurface } from "../ContentsPageSurface";
export default async function EditContentPage({ params }: { params: Promise<{ contentId: string }> }) { const { contentId } = await params; return <ContentsPageSurface contentId={contentId} />; }
