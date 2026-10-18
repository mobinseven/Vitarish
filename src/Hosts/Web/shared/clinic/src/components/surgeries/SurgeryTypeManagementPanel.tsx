"use client";

import { useState } from "react";
import {
  Alert,
  Button,
  FieldError,
  Input,
  Label,
  Surface,
  TextField,
  Tooltip,
} from "@heroui/react";
import { ItemCard, ItemCardGroup } from "@heroui-pro/react";
import {
  AnchorPageHeader,
  EmptyStateSurface,
  ErrorStateSurface,
  FaIcon,
  HeaderIcon,
  LoadingStateSurface,
} from "@vitarish/ui";
import { formatNumber, useLocalizedText } from "../../localization";
import { ApiError } from "../../api/client/apiFetch";
import { useCanPlanSurgeries } from "../../hooks/surgeries/useCanPlanSurgeries";
import { useSurgerySettings } from "../../hooks/surgeries/useSurgerySettings";
import { useSurgerySettingsMutation } from "../../hooks/surgeries/useSurgerySettingsMutation";
import { useSurgeryTypes } from "../../hooks/surgeries/useSurgeryTypes";
import { useSurgeryTypeMutations } from "../../hooks/surgeries/useSurgeryTypeMutations";
import {
  isSurgeryConflictError,
  isSurgeryForbiddenError,
} from "../../hooks/surgeries/useSurgeryMutations";
import { useSurgerySuggestions } from "../../hooks/surgeries/useSurgerySuggestions";
import type {
  SurgerySettings,
  SurgeryType,
} from "../../models/surgeries/surgeryModels";
import { TemplateConfirmDialog } from "../procedures/TemplateConfirmDialog";
import {
  SurgeryTypeEditor,
  type SurgeryTypeEditorValues,
} from "./SurgeryTypeEditor";

function isValidationError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 400;
}

function SurgeryTypeListPane({
  types,
  selectedId,
  isCreating,
  readOnly,
  onSelect,
  onNew,
}: {
  types: readonly SurgeryType[];
  selectedId: string | null;
  isCreating: boolean;
  readOnly: boolean;
  onSelect: (typeId: string) => void;
  onNew: () => void;
}) {
  const { t, locale } = useLocalizedText();
  return (
    <Surface
      className="flex flex-col gap-6 rounded-3xl p-6"
      role="region"
      aria-label={t("clinic.surgeries.types.listTitle")}
    >
      <div className="flex items-center gap-3">
        <HeaderIcon icon="scissors" />
        <div className="min-w-0">
          <h2 className="text-lg font-semibold text-foreground">
            {t("clinic.surgeries.types.listTitle")}
          </h2>
          <p className="text-sm text-muted">
            {t("clinic.surgeries.types.listDescription")}
          </p>
        </div>
      </div>
      {types.length === 0 ? (
        <EmptyStateSurface
          title={t("clinic.surgeries.types.emptyTitle")}
          description={t("clinic.surgeries.types.emptyDescription")}
        />
      ) : (
        <ItemCardGroup className="overflow-hidden" variant="transparent">
          {types.map((type) => (
            <ItemCard<"button">
              key={type.id}
              variant={selectedId === type.id ? "secondary" : "default"}
              onClick={() => onSelect(type.id)}
              aria-label={type.name}
              render={(props) => <button type="button" {...props} />}
            >
              <ItemCard.Icon>
                <FaIcon icon="scissors" aria-hidden="true" />
              </ItemCard.Icon>
              <ItemCard.Content className="min-w-0">
                <ItemCard.Title className="truncate">
                  {type.name}
                </ItemCard.Title>
                <ItemCard.Description className="truncate text-sm">
                  {[
                    t("clinic.surgeries.types.counts.tools", {
                      count: formatNumber(type.tools.length, locale),
                    }),
                    t("clinic.surgeries.types.counts.roles", {
                      count: formatNumber(type.defaultTeamRoles.length, locale),
                    }),
                    t("clinic.surgeries.types.counts.reminders", {
                      count: formatNumber(type.reminderPolicies.length, locale),
                    }),
                  ].join(" · ")}
                </ItemCard.Description>
              </ItemCard.Content>
            </ItemCard>
          ))}
        </ItemCardGroup>
      )}
      {readOnly ? null : (
        <Tooltip>
          <Button className="w-full" variant="primary" onPress={onNew} isDisabled={isCreating}>
            <FaIcon icon="plus" aria-hidden="true" />
            {t("clinic.surgeries.types.newType")}
          </Button>
          <Tooltip.Content>
            {t("clinic.surgeries.types.newTypeTooltip")}
          </Tooltip.Content>
        </Tooltip>
      )}
    </Surface>
  );
}

function CancellationMessageForm({
  settings,
  readOnly,
  onRefetch,
}: {
  settings: SurgerySettings;
  readOnly: boolean;
  onRefetch: () => void;
}) {
  const { t } = useLocalizedText();
  const { update } = useSurgerySettingsMutation();
  const [subject, setSubject] = useState(settings.cancellationSubject);
  const [message, setMessage] = useState(settings.cancellationTextBody);
  const [subjectError, setSubjectError] = useState<string | null>(null);
  const [messageError, setMessageError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);

  const handleSave = () => {
    const trimmedSubject = subject.trim();
    const trimmedMessage = message.trim();
    const nextSubjectError = trimmedSubject
      ? null
      : t("clinic.surgeries.types.cancellation.subjectRequired");
    const nextMessageError = trimmedMessage
      ? null
      : t("clinic.surgeries.types.cancellation.messageRequired");
    setSubjectError(nextSubjectError);
    setMessageError(nextMessageError);
    setSaveError(null);
    if (nextSubjectError || nextMessageError) return;

    update.mutate(
      {
        ...settings,
        cancellationSubject: trimmedSubject,
        cancellationTextBody: trimmedMessage,
      },
      {
        onError: (err) => {
          if (isSurgeryConflictError(err)) {
            setSaveError(t("clinic.surgeries.types.cancellation.conflictReloaded"));
            onRefetch();
            return;
          }
          if (isSurgeryForbiddenError(err)) {
            setSaveError(t("clinic.surgeries.errors.ownerOnly"));
            return;
          }
          if (err instanceof ApiError && err.status === 503) {
            setSaveError(t("clinic.surgeries.types.cancellation.unavailable"));
            return;
          }
          setSaveError(t("clinic.surgeries.types.cancellation.saveFailed"));
        },
      },
    );
  };

  return (
    <div className="flex flex-col gap-4">
      {saveError && (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{saveError}</Alert.Title>
          </Alert.Content>
        </Alert>
      )}
      <TextField
        value={subject}
        onChange={(value) => {
          setSubject(value);
          if (subjectError) setSubjectError(null);
        }}
        isInvalid={!!subjectError}
        isDisabled={readOnly}
        isRequired
        variant="secondary"
      >
        <Label>{t("clinic.surgeries.types.cancellation.subjectLabel")}</Label>
        <Input />
        {subjectError && <FieldError>{subjectError}</FieldError>}
      </TextField>
      <TextField
        value={message}
        onChange={(value) => {
          setMessage(value);
          if (messageError) setMessageError(null);
        }}
        isInvalid={!!messageError}
        isDisabled={readOnly}
        isRequired
        variant="secondary"
      >
        <Label>{t("clinic.surgeries.types.cancellation.messageLabel")}</Label>
        <Input />
        {messageError && <FieldError>{messageError}</FieldError>}
      </TextField>
      <p className="text-sm text-muted">
        {t("clinic.surgeries.types.cancellation.note")}
      </p>
      {readOnly ? null : (
        <div className="flex justify-end">
          <Tooltip>
            <Button
              variant="primary"
              isPending={update.isPending}
              onPress={handleSave}
            >
              <FaIcon icon="floppy-disk" aria-hidden="true" />
              {t("clinic.surgeries.types.cancellation.save")}
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.types.cancellation.saveTooltip")}
            </Tooltip.Content>
          </Tooltip>
        </div>
      )}
    </div>
  );
}

function SurgeryCancellationSettingsSection() {
  const { t } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const { settings, isLoading, error, refetch } = useSurgerySettings();

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex items-center gap-3">
        <HeaderIcon icon="message-slash" />
        <div className="min-w-0">
          <h2 className="text-lg font-semibold text-foreground">
            {t("clinic.surgeries.types.cancellation.title")}
          </h2>
          <p className="text-sm text-muted">
            {t("clinic.surgeries.types.cancellation.description")}
          </p>
        </div>
      </div>
      {isLoading ? (
        <p className="text-sm text-muted">{t("common.states.loading")}</p>
      ) : error ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>
              {t("clinic.surgeries.types.cancellation.loadFailed")}
            </Alert.Title>
            <Alert.Description>
              <Tooltip>
                <Button variant="outline" onPress={() => void refetch()}>
                  <FaIcon icon="rotate-right" aria-hidden="true" />
                  {t("clinic.surgeries.types.retry")}
                </Button>
                <Tooltip.Content>
                  {t("clinic.surgeries.types.retry")}
                </Tooltip.Content>
              </Tooltip>
            </Alert.Description>
          </Alert.Content>
        </Alert>
      ) : settings ? (
        <CancellationMessageForm
          key={settings.concurrencyToken}
          settings={settings}
          readOnly={!canPlan}
          onRefetch={() => void refetch()}
        />
      ) : null}
    </Surface>
  );
}

export function SurgeryTypeManagementPanel() {
  const { t } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const { types, isLoading, error, refetch } = useSurgeryTypes();
  const { create, update, remove } = useSurgeryTypeMutations();
  const suggestions = useSurgerySuggestions();
  const roleSuggestions = suggestions.data?.roles ?? [];
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [blockedDeleteIds, setBlockedDeleteIds] = useState<string[]>([]);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  // Narrow screens show one pane at a time: the type list until a type is opened.
  const [isEditorOpen, setIsEditorOpen] = useState(false);

  // Like the template pages, the editor opens on the first type until another is chosen.
  const selected =
    types.find((type) => type.id === selectedId) ?? types[0] ?? null;
  const showEditor = canPlan && (creating || selected !== null);
  const editorKey = creating ? "new" : (selected?.id ?? "none");
  const deleteBlockedMessage =
    selected && blockedDeleteIds.includes(selected.id)
      ? t("clinic.surgeries.types.defaultCannotDelete")
      : null;

  const describeSaveError = (err: unknown): string => {
    if (isValidationError(err))
      return t("clinic.surgeries.types.validationFailed");
    if (isSurgeryForbiddenError(err))
      return t("clinic.surgeries.errors.ownerOnly");
    if (err instanceof ApiError && err.status === 404)
      return t("clinic.surgeries.types.notFound");
    if (err instanceof ApiError && err.status === 503)
      return t("clinic.surgeries.types.unavailable");
    return t("clinic.surgeries.types.saveFailed");
  };

  const handleSelect = (typeId: string) => {
    if (!canPlan) return;
    setCreating(false);
    setSaveError(null);
    setSelectedId(typeId);
    setIsEditorOpen(true);
  };

  const handleNew = () => {
    setCreating(true);
    setSaveError(null);
    setIsEditorOpen(true);
  };

  const handleBack = () => {
    setCreating(false);
    setSaveError(null);
    setIsEditorOpen(false);
  };

  const handleSave = (values: SurgeryTypeEditorValues) => {
    setSaveError(null);
    if (creating || !selected) {
      create.mutate(values, {
        onSuccess: (created) => {
          setCreating(false);
          setSelectedId(created.id);
        },
        onError: (err) => setSaveError(describeSaveError(err)),
      });
      return;
    }
    update.mutate(
      { ...selected, ...values },
      {
        onSuccess: () => setSaveError(null),
        onError: (err) => {
          if (isSurgeryConflictError(err)) {
            setSaveError(t("clinic.surgeries.types.conflictReloaded"));
            refetch();
          } else {
            setSaveError(describeSaveError(err));
          }
        },
      },
    );
  };

  const handleConfirmDelete = () => {
    const target = types.find((type) => type.id === confirmDeleteId);
    if (!target) {
      setConfirmDeleteId(null);
      return;
    }
    remove.mutate(target, {
      onSuccess: () => {
        setConfirmDeleteId(null);
        setSaveError(null);
      },
      onError: (err) => {
        setConfirmDeleteId(null);
        if (isSurgeryConflictError(err)) {
          setBlockedDeleteIds((ids) =>
            ids.includes(target.id) ? ids : [...ids, target.id],
          );
        } else if (isSurgeryForbiddenError(err)) {
          setSaveError(t("clinic.surgeries.errors.ownerOnly"));
        } else {
          setSaveError(t("clinic.surgeries.types.deleteFailed"));
        }
      },
    });
  };

  return (
    <div className="flex min-h-0 flex-col gap-6">
      <AnchorPageHeader
        icon="scissors"
        title={t("clinic.surgeries.types.title")}
        details={t("clinic.surgeries.types.settingsDescription")}
      />

      {canPlan ? null : (
        <Alert status="default">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("clinic.surgeries.errors.ownerOnly")}</Alert.Title>
          </Alert.Content>
        </Alert>
      )}

      {isLoading ? (
        <LoadingStateSurface label={t("common.states.loading")} />
      ) : error ? (
        <ErrorStateSurface title={t("clinic.surgeries.types.loadFailed")}>
          <Tooltip>
            <Button variant="tertiary" onPress={() => refetch()}>
              <FaIcon icon="rotate-right" aria-hidden="true" />
              {t("clinic.surgeries.types.retry")}
            </Button>
            <Tooltip.Content>
              {t("clinic.surgeries.types.retry")}
            </Tooltip.Content>
          </Tooltip>
        </ErrorStateSurface>
      ) : (
        <div className="flex min-h-0 flex-col gap-4 lg:flex-row">
          <div
            className={
              showEditor
                ? `${isEditorOpen ? "hidden" : "flex"} flex-col lg:flex lg:w-80 lg:shrink-0`
                : "flex flex-1 flex-col"
            }
          >
            <SurgeryTypeListPane
              types={types}
              selectedId={creating ? null : (selected?.id ?? null)}
              isCreating={creating}
              readOnly={!canPlan}
              onSelect={handleSelect}
              onNew={handleNew}
            />
          </div>
          {showEditor ? (
            <div
              className={`${isEditorOpen ? "flex" : "hidden"} min-w-0 flex-1 flex-col lg:flex`}
            >
              <SurgeryTypeEditor
                key={editorKey}
                type={creating ? null : selected}
                roleSuggestions={roleSuggestions}
                isSaving={create.isPending || update.isPending}
                isRemoving={remove.isPending}
                showBack
                saveErrorMessage={saveError}
                deleteBlockedMessage={deleteBlockedMessage}
                onBack={handleBack}
                onSave={handleSave}
                onRequestDelete={() =>
                  selected && setConfirmDeleteId(selected.id)
                }
              />
            </div>
          ) : null}
        </div>
      )}

      <SurgeryCancellationSettingsSection />

      {confirmDeleteId && (
        <TemplateConfirmDialog
          title={t("clinic.surgeries.types.deleteConfirmTitle")}
          message={t("clinic.surgeries.types.deleteConfirmMessage")}
          isPending={remove.isPending}
          onClose={() => setConfirmDeleteId(null)}
          onConfirm={handleConfirmDelete}
        />
      )}
    </div>
  );
}
