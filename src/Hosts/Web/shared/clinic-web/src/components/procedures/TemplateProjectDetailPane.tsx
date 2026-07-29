"use client";
import { useState } from "react";
import {
  Alert,
  Button,
  Chip,
  Disclosure,
  Input,
  Label,
  TextArea,
  TextField,
  Tooltip,
} from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { ItemCard, ItemCardGroup } from "@heroui-pro/react";
import {
  useCreateSeaEvent,
  useCreateSeaProcedure,
  useDeleteSeaProcedure,
  useSeaTemplateProjectDetail,
  useUpdateSeaProcedure,
  type SeaEventReadDto,
  type SeaProcedureReadDto,
} from "@vitarish/sea-web";
import { useLocalizedText } from "../../localization";
import { TemplateConfirmDialog } from "./TemplateConfirmDialog";
import { RoutineStepEditorPane } from "./RoutineStepEditorPane";

export function TemplateProjectDetailPane({
  projectId,
  procedures,
  selectedProcedureId,
  onSelectProcedure,
  events,
  selectedEventId,
  onSelectEvent,
}: {
  projectId: string | null;
  procedures: SeaProcedureReadDto[];
  selectedProcedureId: string | null;
  onSelectProcedure: (id: string | null) => void;
  events: SeaEventReadDto[];
  selectedEventId: string | null;
  onSelectEvent: (id: string | null) => void;
}) {
  const { t } = useLocalizedText();
  const projectQuery = useSeaTemplateProjectDetail(projectId ?? "");
  const project = projectQuery.data;
  const create = useCreateSeaProcedure();
  const update = useUpdateSeaProcedure();
  const remove = useDeleteSeaProcedure();
  const createEvent = useCreateSeaEvent();
  const [editing, setEditing] = useState<SeaProcedureReadDto | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [duplicateError, setDuplicateError] = useState<string | null>(null);
  const [isDuplicating, setIsDuplicating] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  if (!projectId)
    return (
      <p className="p-4 text-sm text-muted">
        {t("clinic.procedures.templateManagement.selectTemplate")}
      </p>
    );
  const begin = (p?: SeaProcedureReadDto) => {
    setEditing(p ?? null);
    setName(p?.name ?? "");
    setDescription(p?.description ?? "");
  };
  const save = () => {
    if (!name.trim()) return;
    const data = { projectId, name, description };
    if (editing)
      update.mutate(
        { ...data, id: editing.id },
        { onSuccess: () => setEditing(null) },
      );
    else
      create.mutate(data, {
        onSuccess: () => {
          setEditing(null);
          setName("");
          setDescription("");
        },
      });
  };
  const duplicate = async (source: SeaProcedureReadDto) => {
    setDuplicateError(null);
    setIsDuplicating(true);
    try {
      const result = await create.mutateAsync({
        projectId,
        name: source.name,
        description: source.description ?? "",
      });
      if (!result.succeeded || !result.entityId) throw new Error("create");
      for (const step of events) {
        const eventResult = await createEvent.mutateAsync({
          data: {
            projectId,
            procedureId: result.entityId,
            title: step.title,
            description: step.description,
          },
        });
        if (!eventResult.succeeded) throw new Error("step");
      }
    } catch {
      setDuplicateError(
        t("clinic.procedures.templateManagement.duplicatePartial"),
      );
    } finally {
      setIsDuplicating(false);
    }
  };
  return (
    <div className="flex flex-col gap-4">
      {editing && (
        <div className="flex flex-col gap-2 rounded-lg border p-3">
          <TextField value={name} onChange={setName}>
            <Label>
              {t("clinic.procedures.templateManagement.routineName")}
            </Label>
            <Input
              placeholder={t(
                "clinic.procedures.templateManagement.routineNamePlaceholder",
              )}
            />
          </TextField>
          <TextField value={description} onChange={setDescription}>
            <Label>
              {t("clinic.procedures.templateManagement.routineDescription")}
            </Label>
            <TextArea
              placeholder={t(
                "clinic.procedures.templateManagement.routineDescriptionPlaceholder",
              )}
            />
          </TextField>
          <Tooltip>
            <Button
              isIconOnly
              aria-label={t("clinic.procedures.templateManagement.pending")}
              isPending={create.isPending || update.isPending}
              onPress={save}
            >
              <FaIcon icon="floppy-disk" aria-hidden="true" />
            </Button>
            <Tooltip.Content>
              {t("clinic.procedures.templateManagement.pending")}
            </Tooltip.Content>
          </Tooltip>
        </div>
      )}
      <ItemCardGroup className="overflow-hidden" variant="secondary">
        <ItemCardGroup.Header>
          <div className="flex items-center justify-between gap-2">
            <ItemCardGroup.Title>
              {t("clinic.procedures.templateManagement.procedures")}
            </ItemCardGroup.Title>
            <Tooltip>
              <Button
                isIconOnly
                aria-label={t(
                  "clinic.procedures.templateManagement.newRoutine",
                )}
                onPress={() => begin()}
              >
                <FaIcon icon="plus" aria-hidden="true" />
              </Button>
              <Tooltip.Content>
                {t("clinic.procedures.templateManagement.newRoutine")}
              </Tooltip.Content>
            </Tooltip>
          </div>
        </ItemCardGroup.Header>
        {procedures.map((p) => (
          <ItemCard key={p.id}>
            <ItemCard.Icon>
              <FaIcon icon="list-check" aria-hidden="true" />
            </ItemCard.Icon>
            <ItemCard.Content className="min-w-0">
              <Tooltip>
                <Button
                  variant={
                    selectedProcedureId === p.id ? "primary" : "secondary"
                  }
                  className="justify-start"
                  onPress={() => onSelectProcedure(p.id)}
                >
                  {p.name}
                </Button>
                <Tooltip.Content>{p.name}</Tooltip.Content>
              </Tooltip>
              <p className="text-sm text-muted">{p.description}</p>
            </ItemCard.Content>
            <ItemCard.Action>
              <div className="flex gap-2">
                <Chip size="sm">
                  {p.stats?.events ?? 0}{" "}
                  {t("clinic.procedures.templateManagement.steps")}
                </Chip>
                <Tooltip>
                  <Button
                    isIconOnly
                    size="sm"
                    aria-label={t(
                      "clinic.procedures.templateManagement.editRoutine",
                    )}
                    onPress={() => begin(p)}
                  >
                    <FaIcon icon="pen" aria-hidden="true" />
                  </Button>
                  <Tooltip.Content>
                    {t("clinic.procedures.templateManagement.editRoutine")}
                  </Tooltip.Content>
                </Tooltip>
                {p.id === selectedProcedureId && (
                  <Tooltip>
                    <Button
                      isIconOnly
                      size="sm"
                      aria-label={t(
                        "clinic.procedures.templateManagement.duplicateRoutine",
                      )}
                      isPending={isDuplicating}
                      isDisabled={isDuplicating}
                      onPress={() => duplicate(p)}
                    >
                      <FaIcon icon="copy" aria-hidden="true" />
                    </Button>
                    <Tooltip.Content>
                      {isDuplicating
                        ? t(
                            "clinic.procedures.templateManagement.duplicatePending",
                          )
                        : t(
                            "clinic.procedures.templateManagement.duplicateRoutine",
                          )}
                    </Tooltip.Content>
                  </Tooltip>
                )}
                <Tooltip>
                  <Button
                    isIconOnly
                    size="sm"
                    aria-label={t(
                      "clinic.procedures.templateManagement.deleteRoutine",
                    )}
                    variant="danger"
                    isDisabled={remove.isPending}
                    onPress={() => setDeleteId(p.id)}
                  >
                    <FaIcon icon="trash" aria-hidden="true" />
                  </Button>
                  <Tooltip.Content>
                    {t("clinic.procedures.templateManagement.deleteRoutine")}
                  </Tooltip.Content>
                </Tooltip>
              </div>
            </ItemCard.Action>
          </ItemCard>
        ))}
      </ItemCardGroup>
      {selectedProcedureId && (
        <div className="flex flex-col gap-3">
          <div>
            <h3 className="text-lg font-semibold">
              {t("clinic.procedures.templateManagement.steps")}
            </h3>
            <p className="text-sm text-muted">
              {t("clinic.procedures.templateManagement.selectStep")}
            </p>
          </div>
          {events.map((e, i) => (
            <Disclosure
              key={e.id}
              className={
                selectedEventId === e.id
                  ? "rounded-2xl border border-accent"
                  : "rounded-2xl border"
              }
              isExpanded={selectedEventId === e.id}
              onExpandedChange={(expanded) =>
                onSelectEvent(expanded ? e.id : null)
              }
            >
              <Disclosure.Heading>
                <Button
                  slot="trigger"
                  variant="ghost"
                  className="h-auto w-full justify-start gap-3 p-4 text-start"
                >
                  <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-accent-soft text-lg font-semibold text-accent">
                    {i + 1}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-base font-medium">
                      {e.title}
                    </span>
                    <span className="mt-1 block truncate text-sm text-muted">
                      {e.description ||
                        t(
                          "clinic.procedures.templateManagement.stepDescription",
                        )}
                    </span>
                  </span>
                  <Disclosure.Indicator className="ms-auto" />
                </Button>
              </Disclosure.Heading>
              <Disclosure.Content>
                <Disclosure.Body className="p-3">
                  {selectedEventId === e.id && (
                    <RoutineStepEditorPane
                      procedureId={selectedProcedureId}
                      eventId={e.id}
                    />
                  )}
                </Disclosure.Body>
              </Disclosure.Content>
            </Disclosure>
          ))}
        </div>
      )}
      {duplicateError && (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{duplicateError}</Alert.Title>
          </Alert.Content>
        </Alert>
      )}
      {projectQuery.error && (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {t("clinic.procedures.templateManagement.error")}
            </Alert.Title>
          </Alert.Content>
        </Alert>
      )}
      {deleteId && (
        <TemplateConfirmDialog
          title={t("clinic.procedures.templateManagement.deleteRoutine")}
          message={t(
            "clinic.procedures.templateManagement.deleteRoutineConfirm",
          )}
          isPending={remove.isPending}
          onClose={() => setDeleteId(null)}
          onConfirm={() =>
            remove.mutate(deleteId, { onSuccess: () => setDeleteId(null) })
          }
        />
      )}
    </div>
  );
}
