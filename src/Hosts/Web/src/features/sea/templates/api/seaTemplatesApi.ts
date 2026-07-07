import { seaGetProject, seaListProjects } from "@/api/generated/sea/sea";
import { mapSeaProject } from "../../shared/models/seaReadDtoMappers";
import type { SeaProjectReadDto } from "../../shared/models/seaStatModels";

export async function listSeaTemplateProjects(): Promise<SeaProjectReadDto[]> {
  const projects = await seaListProjects();
  return projects.map(mapSeaProject).filter((project) => project.template);
}

export async function getSeaTemplateProject(id: string): Promise<SeaProjectReadDto> {
  return mapSeaProject(await seaGetProject(id));
}
