"use client";

import type { ReactNode } from "react";
import { useState } from "react";
import { Button, Chip, Input, Label, TextArea, TextField, Tooltip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import { useCreateSeaProject, type SeaProjectReadDto } from "@vitarish/sea-web";
import { useLocalizedText } from "../../localization";

export function TemplateProjectListPane({ projects, selectedProjectId, onSelect }: { projects: SeaProjectReadDto[]; selectedProjectId: string | null; onSelect: (id: string) => void }) {
  const { t } = useLocalizedText(); const create = useCreateSeaProject(); const [query, setQuery] = useState(""); const [showForm, setShowForm] = useState(false); const [name, setName] = useState(""); const [description, setDescription] = useState("");
  const visible = projects.filter((project) => project.name.toLowerCase().includes(query.toLowerCase()));
  return <><Widget.Header><Widget.Title>{t("clinic.procedures.templateManagement.title")}</Widget.Title></Widget.Header><Widget.Content className="flex flex-col gap-3">
    <div className="flex items-center gap-2"><FaIcon icon="search" aria-hidden="true" /><Input value={query} onChange={(e) => setQuery(e.target.value)} placeholder={t("clinic.procedures.templateManagement.searchPlaceholder")} aria-label={t("clinic.procedures.templateManagement.searchPlaceholder")} /></div>
    <Tooltip><Button isDisabled={create.isPending} onPress={() => setShowForm((value) => !value)}><FaIcon icon={showForm ? "xmark" : "plus"} aria-hidden="true" />{t("clinic.procedures.templateManagement.newTemplate")}</Button><Tooltip.Content>{t("clinic.procedures.templateManagement.newTemplate")}</Tooltip.Content></Tooltip>
    {showForm && <form className="flex flex-col gap-2" onSubmit={(e) => { e.preventDefault(); if (!name.trim()) return; create.mutate({ name, description, template: true }, { onSuccess: () => { setShowForm(false); setName(""); setDescription(""); } }); }}><TextField value={name} onChange={setName}><Label>{t("clinic.procedures.templateManagement.projectName")}</Label><Input placeholder={t("clinic.procedures.templateManagement.projectNamePlaceholder")} /></TextField><TextField value={description} onChange={setDescription}><Label>{t("clinic.procedures.templateManagement.projectDescription")}</Label><TextArea placeholder={t("clinic.procedures.templateManagement.projectDescriptionPlaceholder")} /></TextField><Tooltip><Button type="submit" isDisabled={create.isPending}><FaIcon icon="floppy-disk" aria-hidden="true" />{create.isPending ? t("clinic.procedures.templateManagement.createPending") : t("clinic.procedures.templateManagement.newTemplate")}</Button><Tooltip.Content>{t("clinic.procedures.templateManagement.newTemplate")}</Tooltip.Content></Tooltip></form>}
    <div className="flex flex-col gap-1">{visible.map((project) => <Tooltip key={project.id}><Button variant={selectedProjectId === project.id ? "primary" : "secondary"} className="justify-between" onPress={() => onSelect(project.id)}><span className="truncate"><FaIcon icon="folder" aria-hidden="true" /> {project.name}</span><span className="flex gap-1"><Chip size="sm">{project.stats?.procedures ?? 0}</Chip><Chip size="sm">{project.stats?.events ?? 0}</Chip></span></Button><Tooltip.Content>{project.name}</Tooltip.Content></Tooltip>)}</div>
    {!visible.length && <p className="text-sm text-muted">{t("clinic.procedures.templateManagement.noTemplates")}</p>}
  </Widget.Content></>;
}

export function TemplatePaneContent({ children }: { children: ReactNode }) { return <Widget.Content>{children}</Widget.Content>; }
