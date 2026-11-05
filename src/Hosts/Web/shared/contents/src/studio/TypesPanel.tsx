"use client";

import { Surface } from "@heroui/react";
import { ApiError } from "@vitarish/api-client";
import { useLocalizedText } from "@vitarish/localization";
import { useEffect, useState } from "react";
import {
  contentFieldKind,
  contentFieldKinds,
  type ContentFieldDefinition,
  type ContentFieldKind,
} from "./contentsStudioApi";
import { StudioButton, StudioFailure, StudioInput, StudioSelect, StudioState, StudioTextArea } from "./StudioControls";
import {
  useContentTypes,
  useCreateContentType,
  useUpdateContentTypeSchema,
} from "./useContentsStudio";

const groupChildKinds = contentFieldKinds.filter(
  (kind) => kind !== contentFieldKind.group && kind !== contentFieldKind.list,
);

type EditableFieldDefinition = Omit<ContentFieldDefinition, "fields"> & {
  editorId: string;
  isPersisted: boolean;
  fields?: EditableFieldDefinition[] | null;
};

let nextEditorFieldId = 0;

function editorField(
  field: ContentFieldDefinition,
  isPersisted: boolean,
): EditableFieldDefinition {
  const { fields, ...definition } = field;
  nextEditorFieldId += 1;
  return {
    ...definition,
    editorId: `content-field-${nextEditorFieldId}`,
    isPersisted,
    ...(fields ? { fields: fields.map((child) => editorField(child, isPersisted)) } : {}),
  };
}

function wireField(field: EditableFieldDefinition): ContentFieldDefinition {
  const { editorId: _editorId, isPersisted: _isPersisted, ...definition } = field;
  return {
    ...definition,
    ...(field.fields ? { fields: field.fields.map(wireField) } : {}),
  };
}

function emptyField(kind: ContentFieldKind = contentFieldKind.text): ContentFieldDefinition {
  return {
    key: "",
    label: "",
    kind,
    required: false,
    localized: false,
    maxLength: null,
    ...(kind === contentFieldKind.group ? { fields: [] } : {}),
    ...(kind === contentFieldKind.list ? { item: emptyField(contentFieldKind.text) } : {}),
  };
}

function listItem(field: ContentFieldDefinition): ContentFieldDefinition {
  return field.item ?? {
    key: field.key,
    label: field.label,
    kind: contentFieldKind.text,
    required: false,
    localized: false,
    maxLength: field.maxLength ?? null,
  };
}

function withKind(
  field: EditableFieldDefinition,
  kind: ContentFieldKind,
): EditableFieldDefinition {
  const next: EditableFieldDefinition = { ...field, kind };
  if (kind === contentFieldKind.group) next.fields = field.fields ?? [];
  else delete next.fields;
  if (kind === contentFieldKind.list) next.item = listItem(field);
  else delete next.item;
  return next;
}

function move<T>(items: T[], index: number, offset: number): T[] {
  const target = index + offset;
  if (target < 0 || target >= items.length) return items;
  const next = [...items];
  [next[index], next[target]] = [next[target], next[index]];
  return next;
}

function schemaFields(fields: EditableFieldDefinition[]): ContentFieldDefinition[] {
  return fields.map((field) => {
    const definition = wireField(field);
    return definition.kind === contentFieldKind.list
      ? { ...definition, item: definition.item ?? listItem(definition) }
      : definition;
  });
}

function hasValidationError(error: unknown, path: string): boolean {
  if (!(error instanceof ApiError)) return false;
  const normalizedPath = path.toLowerCase().replaceAll("[", ".").replaceAll("]", "");
  return Object.entries(error.problem.errors ?? {}).some(([key]) => {
    const normalizedKey = key.toLowerCase().replaceAll("[", ".").replaceAll("]", "");
    return normalizedKey === normalizedPath ||
      normalizedKey.endsWith(`.${normalizedPath}`) ||
      normalizedKey.startsWith(`${normalizedPath}.`);
  });
}

export function TypesPanel() {
  const { t } = useLocalizedText();
  const typesQuery = useContentTypes();
  const createType = useCreateContentType();
  const updateSchema = useUpdateContentTypeSchema();
  const [selectedId, setSelectedId] = useState("");
  const [creating, setCreating] = useState(false);
  const [key, setKey] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [fields, setFields] = useState<EditableFieldDefinition[]>([]);

  const selectedType = typesQuery.data?.find((type) => type.id === selectedId);

  useEffect(() => {
    if (creating) return;
    if (!selectedId && typesQuery.data?.[0]) {
      setSelectedId(typesQuery.data[0].id);
      return;
    }
    if (selectedType) {
      setKey(selectedType.key);
      setName(selectedType.name);
      setDescription(selectedType.description ?? "");
      setFields(selectedType.fields.map((field) => editorField(field, true)));
    }
  }, [creating, selectedId, selectedType, typesQuery.data]);

  const failure = creating ? createType.error : updateSchema.error;
  const pending = createType.isPending || updateSchema.isPending;
  const validationFor = (path: string) => hasValidationError(failure, path)
    ? t("contentsStudio.validation")
    : undefined;
  const options = (kinds: readonly ContentFieldKind[]) =>
    kinds.map((kind) => ({ id: kind, label: t(`contentsStudio.${kind}`) }));
  const setField = (index: number, update: (field: EditableFieldDefinition) => EditableFieldDefinition) =>
    setFields((current) => current.map((field, currentIndex) => currentIndex === index ? update(field) : field));
  const setChild = (
    fieldIndex: number,
    childIndex: number,
    update: (field: EditableFieldDefinition) => EditableFieldDefinition,
  ) => setField(fieldIndex, (field) => ({
    ...field,
    fields: (field.fields ?? []).map((child, currentIndex) => currentIndex === childIndex ? update(child) : child),
  }));
  const startCreate = () => {
    createType.reset();
    updateSchema.reset();
    setCreating(true);
    setSelectedId("");
    setKey("");
    setName("");
    setDescription("");
    setFields([]);
  };
  const cancelCreate = () => {
    setCreating(false);
    setSelectedId(typesQuery.data?.[0]?.id ?? "");
  };
  const save = () => {
    createType.reset();
    updateSchema.reset();
    if (creating) {
      createType.mutate({ key, name, description: description || null, fields: schemaFields(fields) }, {
        onSuccess: (outcome) => {
          setCreating(false);
          setSelectedId(outcome.id);
          void typesQuery.refetch();
        },
      });
    } else if (selectedType) {
      updateSchema.mutate({ id: selectedType.id, input: { fields: schemaFields(fields) } });
    }
  };

  if (typesQuery.isLoading) return <StudioState>{t("contentsStudio.loading")}</StudioState>;
  if (typesQuery.isError) return <StudioFailure error={typesQuery.error} />;

  return <div className="flex flex-col gap-6">
    <Surface className="flex flex-col gap-5 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-lg font-semibold">{t("contentsStudio.types")}</h2>
        <StudioButton label={t("contentsStudio.newType")} icon="fa-plus" onPress={startCreate} />
      </div>
      {!creating && (typesQuery.data?.length ?? 0) > 0 && <StudioSelect
        label={t("contentsStudio.type")}
        value={selectedId}
        onChange={(id) => {
          createType.reset();
          updateSchema.reset();
          setSelectedId(id);
        }}
        options={(typesQuery.data ?? []).map((type) => ({ id: type.id, label: type.name }))}
      />}
      {!creating && !selectedType && <StudioState>{t("contentsStudio.empty")}</StudioState>}
      {creating && <>
        <StudioInput label={t("contentsStudio.key")} value={key} onChange={setKey} error={validationFor("key")} />
        <StudioInput label={t("contentsStudio.name")} value={name} onChange={setName} error={validationFor("name")} />
        <StudioTextArea label={t("contentsStudio.description")} value={description} onChange={setDescription} error={validationFor("description")} />
      </>}
      {(creating || selectedType) && <>
        {!creating && selectedType && <>
          <StudioInput label={t("contentsStudio.key")} value={key} onChange={setKey} readOnly />
          <StudioInput label={t("contentsStudio.name")} value={name} onChange={setName} readOnly />
          <StudioTextArea label={t("contentsStudio.description")} value={description} onChange={() => undefined} readOnly />
        </>}
        <div className="flex flex-col gap-4">
          <h3 className="text-lg font-semibold">{t("contentsStudio.fields")}</h3>
          {validationFor("fields") && <p role="alert">{validationFor("fields")}</p>}
          {fields.map((field, index) => {
            const persisted = field.isPersisted;
            const errorPath = `fields[${index}]`;
            const kindValue = (value: string) => value as ContentFieldKind;
            return <Surface key={field.editorId} variant="secondary" className="flex flex-col gap-4 rounded-2xl p-4">
              <div className="flex flex-wrap items-center gap-2">
                <StudioButton label={t("contentsStudio.up")} icon="fa-arrow-up" disabled={index === 0} onPress={() => setFields((current) => move(current, index, -1))} />
                <StudioButton label={t("contentsStudio.down")} icon="fa-arrow-down" disabled={index === fields.length - 1} onPress={() => setFields((current) => move(current, index, 1))} />
                {!persisted && <StudioButton label={t("contentsStudio.remove")} icon="fa-trash" onPress={() => setFields((current) => current.filter((_item, currentIndex) => currentIndex !== index))} />}
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <StudioInput label={t("contentsStudio.key")} value={field.key} readOnly={persisted} onChange={(value) => setField(index, (current) => ({ ...current, key: value, ...(!persisted && current.kind === contentFieldKind.list && current.item ? { item: { ...current.item, key: value } } : {}) }))} error={validationFor(`${errorPath}.key`)} />
                <StudioInput label={t("contentsStudio.label")} value={field.label} onChange={(value) => setField(index, (current) => ({ ...current, label: value, ...(!persisted && current.kind === contentFieldKind.list && current.item ? { item: { ...current.item, label: value } } : {}) }))} error={validationFor(`${errorPath}.label`)} />
                {persisted
                  ? <StudioInput label={t("contentsStudio.kind")} value={t(`contentsStudio.${field.kind}`)} readOnly onChange={() => undefined} />
                  : <StudioSelect label={t("contentsStudio.kind")} value={field.kind} onChange={(value) => setField(index, (current) => withKind(current, kindValue(value)))} options={options(contentFieldKinds)} />}
                <StudioInput label={t("contentsStudio.maxLength")} type="number" value={field.maxLength == null ? "" : String(field.maxLength)} onChange={(value) => setField(index, (current) => ({ ...current, maxLength: value.trim() ? Number(value) : null }))} error={validationFor(`${errorPath}.maxLength`)} />
                <label className="flex min-h-12 items-center gap-3 text-base"><input className="size-5" type="checkbox" checked={field.required} onChange={(event) => setField(index, (current) => ({ ...current, required: event.target.checked }))} /><span>{t("contentsStudio.required")}</span></label>
                <label className="flex min-h-12 items-center gap-3 text-base"><input className="size-5" type="checkbox" checked={field.localized} disabled={persisted} onChange={(event) => setField(index, (current) => ({ ...current, localized: event.target.checked }))} /><span>{t("contentsStudio.localized")}</span></label>
              </div>
              {field.kind === contentFieldKind.list && (() => {
                const item = listItem(field);
                return persisted
                  ? <StudioInput label={t("contentsStudio.itemKind")} value={t(`contentsStudio.${item.kind}`)} readOnly onChange={() => undefined} />
                  : <div className="flex flex-col gap-2"><StudioSelect label={t("contentsStudio.itemKind")} value={item.kind} onChange={(value) => setField(index, (current) => ({ ...current, item: { ...listItem(current), kind: kindValue(value) } }))} options={options(contentFieldKinds.filter((kind) => kind !== contentFieldKind.list && kind !== contentFieldKind.group))} />{validationFor(`${errorPath}.item`) && <p role="alert">{validationFor(`${errorPath}.item`)}</p>}</div>;
              })()}
              {field.kind === contentFieldKind.group && <div className="flex flex-col gap-4">
                <h4 className="text-base font-semibold">{t("contentsStudio.children")}</h4>
                {(field.fields ?? []).map((child, childIndex) => {
                  const childPersisted = child.isPersisted;
                  const childPath = `${errorPath}.fields[${childIndex}]`;
                  return <div key={child.editorId} className="flex flex-col gap-3 rounded-2xl p-3">
                    <div className="flex flex-wrap items-center gap-2">
                      <StudioButton label={t("contentsStudio.up")} icon="fa-arrow-up" disabled={childIndex === 0} onPress={() => setField(index, (current) => ({ ...current, fields: move(current.fields ?? [], childIndex, -1) }))} />
                      <StudioButton label={t("contentsStudio.down")} icon="fa-arrow-down" disabled={childIndex === (field.fields?.length ?? 0) - 1} onPress={() => setField(index, (current) => ({ ...current, fields: move(current.fields ?? [], childIndex, 1) }))} />
                      {!childPersisted && <StudioButton label={t("contentsStudio.remove")} icon="fa-trash" onPress={() => setField(index, (current) => ({ ...current, fields: (current.fields ?? []).filter((_item, currentIndex) => currentIndex !== childIndex) }))} />}
                    </div>
                    <div className="grid gap-4 md:grid-cols-2">
                      <StudioInput label={t("contentsStudio.key")} value={child.key} readOnly={childPersisted} onChange={(value) => setChild(index, childIndex, (current) => ({ ...current, key: value }))} error={validationFor(`${childPath}.key`)} />
                      <StudioInput label={t("contentsStudio.label")} value={child.label} onChange={(value) => setChild(index, childIndex, (current) => ({ ...current, label: value }))} error={validationFor(`${childPath}.label`)} />
                      {childPersisted
                        ? <StudioInput label={t("contentsStudio.kind")} value={t(`contentsStudio.${child.kind}`)} readOnly onChange={() => undefined} />
                        : <StudioSelect label={t("contentsStudio.kind")} value={child.kind} onChange={(value) => setChild(index, childIndex, (current) => withKind(current, kindValue(value)))} options={options(groupChildKinds)} />}
                      <StudioInput label={t("contentsStudio.maxLength")} type="number" value={child.maxLength == null ? "" : String(child.maxLength)} onChange={(value) => setChild(index, childIndex, (current) => ({ ...current, maxLength: value.trim() ? Number(value) : null }))} error={validationFor(`${childPath}.maxLength`)} />
                      <label className="flex min-h-12 items-center gap-3 text-base"><input className="size-5" type="checkbox" checked={child.required} onChange={(event) => setChild(index, childIndex, (current) => ({ ...current, required: event.target.checked }))} /><span>{t("contentsStudio.required")}</span></label>
                      <label className="flex min-h-12 items-center gap-3 text-base"><input className="size-5" type="checkbox" checked={child.localized} disabled={childPersisted} onChange={(event) => setChild(index, childIndex, (current) => ({ ...current, localized: event.target.checked }))} /><span>{t("contentsStudio.localized")}</span></label>
                    </div>
                  </div>;
                })}
                <StudioButton label={t("contentsStudio.add")} icon="fa-plus" onPress={() => setField(index, (current) => ({ ...current, fields: [...(current.fields ?? []), editorField(emptyField(groupChildKinds[0]), false)] }))} />
              </div>}
            </Surface>;
          })}
          <StudioButton label={t("contentsStudio.add")} icon="fa-plus" onPress={() => setFields((current) => [...current, editorField(emptyField(), false)])} />
        </div>
        {failure && <StudioFailure error={failure} />}
        <div className="flex flex-wrap gap-3">
          <StudioButton label={t("contentsStudio.save")} icon="fa-floppy-disk" disabled={pending} onPress={save} />
          {creating && <StudioButton label={t("contentsStudio.cancel")} icon="fa-xmark" disabled={pending} onPress={cancelCreate} />}
        </div>
      </>}
    </Surface>
  </div>;
}
