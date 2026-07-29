"use client";

import type { ReactNode } from "react";
import { useState } from "react";
import {
  Button,
  Chip,
  Input,
  Label,
  TextArea,
  TextField,
  Tooltip,
} from "@heroui/react";
import {
  ItemCard,
  ItemCardGroup,
  PressableFeedback,
  Widget,
} from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import { useCreateSeaProject, type SeaProjectReadDto } from "@vitarish/sea-web";
import { useLocalizedText } from "../../localization";

export function TemplateProjectListPane({
  projects,
  selectedProjectId,
  onSelect,
}: {
  projects: SeaProjectReadDto[];
  selectedProjectId: string | null;
  onSelect: (id: string) => void;
}) {
  const { t } = useLocalizedText();
  const create = useCreateSeaProject();
  const [query, setQuery] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const visible = projects.filter((project) =>
    project.name.toLowerCase().includes(query.toLowerCase()),
  );
  return (
    <>
      <ItemCardGroup className="overflow-hidden" variant="transparent">
        <ItemCardGroup.Header>
          <ItemCardGroup.Title>
            {t("clinic.procedures.templateManagement.projects")}
          </ItemCardGroup.Title>
          <div className="flex items-center gap-2">
            <FaIcon icon="search" aria-hidden="true" />
            <Input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={t(
                "clinic.procedures.templateManagement.searchPlaceholder",
              )}
              aria-label={t(
                "clinic.procedures.templateManagement.searchPlaceholder",
              )}
            />
            <Tooltip>
              <Button
                isIconOnly
                aria-label={t(
                  "clinic.procedures.templateManagement.newTemplate",
                )}
                isDisabled={create.isPending}
                onPress={() => setShowForm((value) => !value)}
              >
                <FaIcon icon={showForm ? "xmark" : "plus"} aria-hidden="true" />
              </Button>
              <Tooltip.Content>
                {showForm
                  ? t("common.actions.cancel")
                  : t("clinic.procedures.templateManagement.newTemplate")}
              </Tooltip.Content>
            </Tooltip>
          </div>
        </ItemCardGroup.Header>
        {showForm && (
          <form
            className="flex flex-col gap-2 p-3"
            onSubmit={(e) => {
              e.preventDefault();
              if (!name.trim()) return;
              create.mutate(
                { name, description, template: true },
                {
                  onSuccess: () => {
                    setShowForm(false);
                    setName("");
                    setDescription("");
                  },
                },
              );
            }}
          >
            <TextField value={name} onChange={setName}>
              <Label>
                {t("clinic.procedures.templateManagement.projectName")}
              </Label>
              <Input
                placeholder={t(
                  "clinic.procedures.templateManagement.projectNamePlaceholder",
                )}
              />
            </TextField>
            <TextField value={description} onChange={setDescription}>
              <Label>
                {t("clinic.procedures.templateManagement.projectDescription")}
              </Label>
              <TextArea
                placeholder={t(
                  "clinic.procedures.templateManagement.projectDescriptionPlaceholder",
                )}
              />
            </TextField>
            <Tooltip>
              <Button
                isIconOnly
                type="submit"
                aria-label={t(
                  "clinic.procedures.templateManagement.newTemplate",
                )}
                isDisabled={create.isPending}
              >
                <FaIcon icon="floppy-disk" aria-hidden="true" />
              </Button>
              <Tooltip.Content>
                {create.isPending
                  ? t("clinic.procedures.templateManagement.createPending")
                  : t("clinic.procedures.templateManagement.newTemplate")}
              </Tooltip.Content>
            </Tooltip>
          </form>
        )}

        {visible.map((project) => (
          <ItemCard<"button">
            key={project.id}
            variant={selectedProjectId === project.id ? "secondary" : "default"}
            onClick={() => onSelect(project.id)}
            aria-label={project.name}
            render={(props) => <button type="button" {...props} />}
          >
            <ItemCard.Icon>
              <FaIcon icon="folder" aria-hidden="true" />
            </ItemCard.Icon>
            <ItemCard.Content className="min-w-0">
              <ItemCard.Title className="truncate">
                {project.name}
              </ItemCard.Title>
            </ItemCard.Content>
            <ItemCard.Action>
              <div className="pointer-events-none flex gap-1">
                <Chip size="sm">{project.stats?.procedures ?? 0}</Chip>
                <Chip size="sm">{project.stats?.events ?? 0}</Chip>
              </div>
            </ItemCard.Action>
          </ItemCard>
        ))}
      </ItemCardGroup>
      {!visible.length && (
        <p className="text-sm text-muted">
          {t("clinic.procedures.templateManagement.noTemplates")}
        </p>
      )}
    </>
  );
}

export function TemplatePaneContent({ children }: { children: ReactNode }) {
  return <Widget.Content>{children}</Widget.Content>;
}
