"use client";

import { useState, useCallback, useMemo } from "react";
import { getLocalTimeZone } from "@internationalized/date";
import { Button, Input, Label, Surface, TextField } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import {
  EmptyStateSurface as ClinicEmptyState,
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
  HeaderIcon,
  actionRoleClassName,
} from "@vitarish/ui";
import type { NavigationIntent } from "../../../navigation";
import { formatNumber, useLocalizedText } from "../../../localization/index";
import {
  useServiceTypes,
  useCreateServiceType,
  useUpdateServiceType,
  useDeleteServiceType,
} from "../../../hooks/service-types/useServiceTypes";
import {
  useServices,
  useCreateService,
  useUpdateService,
  useDeleteService,
} from "../../../hooks/services/useServices";
import {
  useServiceProviders,
  useCreateServiceProvider,
  useUpdateServiceProvider,
  useDeleteServiceProvider,
} from "../../../hooks/service-providers/useServiceProviders";
import {
  useOwnerShifts,
  useCreateShift,
  useDeleteShift,
} from "../../../hooks/appointments/shifts/useOwnerShifts";

interface ServiceConfigurationWorkspaceProps {
  onNavigate: (intent: NavigationIntent) => void;
}

type ActiveSection = "types" | "services" | "providers" | "shifts";

export function ServiceConfigurationWorkspace({
  onNavigate,
}: ServiceConfigurationWorkspaceProps) {
  const { t } = useLocalizedText();
  const [activeSection, setActiveSection] = useState<ActiveSection>("types");

  // Data queries
  const serviceTypes = useServiceTypes();
  const services = useServices();
  const serviceProviders = useServiceProviders();
  const shifts = useOwnerShifts();

  // Mutations
  const createServiceType = useCreateServiceType();
  const updateServiceType = useUpdateServiceType();
  const deleteServiceType = useDeleteServiceType();

  const createService = useCreateService();
  const updateService = useUpdateService();
  const deleteService = useDeleteService();

  const createServiceProvider = useCreateServiceProvider();
  const updateServiceProvider = useUpdateServiceProvider();
  const deleteServiceProvider = useDeleteServiceProvider();

  const createShift = useCreateShift();
  const deleteShift = useDeleteShift();

  // Summary counts
  const summary = useMemo(
    () => ({
      types: serviceTypes.data?.length ?? 0,
      services: services.data?.length ?? 0,
      providers: serviceProviders.data?.length ?? 0,
      shifts: shifts.data?.length ?? 0,
    }),
    [serviceTypes.data, services.data, serviceProviders.data, shifts.data],
  );

  const isLoading =
    serviceTypes.isLoading ||
    services.isLoading ||
    serviceProviders.isLoading ||
    shifts.isLoading;
  const hasError =
    serviceTypes.error ||
    services.error ||
    serviceProviders.error ||
    shifts.error;

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (hasError) {
    return <ClinicErrorState title={t("clinic.appointments.settings.title")} />;
  }

  return (
    <section className="flex flex-col gap-6" aria-labelledby="workspace-title">
      <header>
        <div className="flex items-center gap-4">
          <HeaderIcon icon="sliders" level="page" />
          <div className="min-w-0">
            <h1 id="workspace-title" className="text-2xl font-bold">
              {t("clinic.appointments.settings.title")}
            </h1>
            <p className="text-sm text-muted">
              {t("clinic.appointments.settings.description")}
            </p>
          </div>
        </div>
      </header>

      {/* Summary cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryCard
          count={summary.types}
          icon="tags"
          label={t("clinic.serviceTypes.title")}
          isActive={activeSection === "types"}
          onClick={() => setActiveSection("types")}
        />
        <SummaryCard
          count={summary.services}
          icon="briefcase-medical"
          label={t("clinic.services.title")}
          isActive={activeSection === "services"}
          onClick={() => setActiveSection("services")}
        />
        <SummaryCard
          count={summary.providers}
          icon="user-doctor"
          label={t("clinic.serviceProviders.title")}
          isActive={activeSection === "providers"}
          onClick={() => setActiveSection("providers")}
        />
        <SummaryCard
          count={summary.shifts}
          icon="clock"
          label={t("clinic.appointments.shifts.title")}
          isActive={activeSection === "shifts"}
          onClick={() => setActiveSection("shifts")}
        />
      </div>

      {/* Active section content */}
      {activeSection === "types" && (
        <ServiceTypesSection
          items={serviceTypes.data ?? []}
          isCreating={createServiceType.isPending}
          isUpdating={updateServiceType.isPending}
          isDeleting={deleteServiceType.isPending}
          onCreate={(name, description) =>
            createServiceType.mutate({
              name,
              description: description || undefined,
            })
          }
          onUpdate={(id, name, description) =>
            updateServiceType.mutate({
              id,
              name,
              description: description || undefined,
            })
          }
          onDelete={(id) => deleteServiceType.mutate(id)}
          onNavigate={onNavigate}
        />
      )}

      {activeSection === "services" && (
        <ServicesSection
          items={services.data ?? []}
          serviceTypes={serviceTypes.data ?? []}
          isCreating={createService.isPending}
          isUpdating={updateService.isPending}
          isDeleting={deleteService.isPending}
          onCreate={(name, description, serviceTypeId) =>
            createService.mutate({
              name,
              description: description || undefined,
              serviceTypeId,
            })
          }
          onUpdate={(id, name, description, serviceTypeId) =>
            updateService.mutate({
              id,
              name,
              description: description || undefined,
              serviceTypeId,
            })
          }
          onDelete={(id) => deleteService.mutate(id)}
          onNavigate={onNavigate}
        />
      )}

      {activeSection === "providers" && (
        <ProvidersSection
          items={serviceProviders.data ?? []}
          services={services.data ?? []}
          isCreating={createServiceProvider.isPending}
          isUpdating={updateServiceProvider.isPending}
          isDeleting={deleteServiceProvider.isPending}
          onCreate={(name, description) =>
            createServiceProvider.mutate({
              name,
              description: description || undefined,
            })
          }
          onUpdate={(id, name, description) =>
            updateServiceProvider.mutate({
              id,
              name,
              description: description || undefined,
            })
          }
          onDelete={(id) => deleteServiceProvider.mutate(id)}
          onNavigate={onNavigate}
        />
      )}

      {activeSection === "shifts" && (
        <ShiftsSection
          items={shifts.data ?? []}
          providers={serviceProviders.data ?? []}
          services={services.data ?? []}
          isCreating={createShift.isPending}
          isDeleting={deleteShift.isPending}
          onCreate={(shift) => createShift.mutate(shift)}
          onDelete={(id) => deleteShift.mutate(id)}
          onNavigate={onNavigate}
        />
      )}
    </section>
  );
}

// Summary card component
function SummaryCard({
  count,
  icon,
  label,
  isActive,
  onClick,
}: {
  count: number;
  icon: string;
  label: string;
  isActive: boolean;
  onClick: () => void;
}) {
  const { locale } = useLocalizedText();
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={isActive}
      className={`flex items-center gap-4 rounded-2xl p-4 text-start transition-colors ${
        isActive
          ? "bg-accent text-accent-foreground shadow-lg shadow-accent/30"
          : "bg-surface hover:bg-accent-soft"
      }`}
    >
      <HeaderIcon icon={icon} tone={isActive ? "inverse" : "accent"} />
      <span className="flex flex-col">
        <span className="text-2xl font-bold tabular-nums">{formatNumber(count, locale)}</span>
        <span className={`text-sm ${isActive ? "text-accent-foreground/85" : "text-muted"}`}>{label}</span>
      </span>
    </button>
  );
}

// Service Types section
function ServiceTypesSection({
  items,
  isCreating,
  isUpdating,
  isDeleting,
  onCreate,
  onUpdate,
  onDelete,
  onNavigate,
}: {
  items: readonly { id: string; name: string; description?: string | null }[];
  isCreating: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
  onCreate: (name: string, description: string) => void;
  onUpdate: (id: string, name: string, description: string) => void;
  onDelete: (id: string) => void;
  onNavigate: (intent: NavigationIntent) => void;
}) {
  const { t } = useLocalizedText();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  const handleCreate = useCallback(
    (name: string, description: string) => {
      onCreate(name, description);
      setCreating(false);
    },
    [onCreate],
  );

  const handleUpdate = useCallback(
    (id: string, name: string, description: string) => {
      onUpdate(id, name, description);
      setEditingId(null);
    },
    [onUpdate],
  );

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="tags" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.serviceTypes.title")}</h2>
          </div>
        </div>
        <Button className={actionRoleClassName("create")}
          variant="primary"
          onPress={() => setCreating(true)}
          isDisabled={creating || isCreating}
        >
          {t("common.actions.new")}
        </Button>
      </div>

      <div className="flex flex-col gap-4">
        {creating && (
          <InlineEditor
            initialValue={{ name: "", description: "" }}
            isPending={isCreating}
            onSubmit={(value) => handleCreate(value.name, value.description)}
            onCancel={() => setCreating(false)}
          />
        )}

        {items.length === 0 && !creating ? (
          <ClinicEmptyState title={t("clinic.serviceTypes.empty")} />
        ) : (
          <ListView aria-label={t("clinic.serviceTypes.title")} variant="secondary">
            {items.map((item) => (
              <ListView.Item key={item.id} id={item.id} textValue={item.name}>
                <ListView.ItemContent>
                  {editingId === item.id ? (
                    <InlineEditor
                      initialValue={{
                        name: item.name,
                        description: item.description ?? "",
                      }}
                      isPending={isUpdating}
                      onSubmit={(value) =>
                        handleUpdate(item.id, value.name, value.description)
                      }
                      onCancel={() => setEditingId(null)}
                    />
                  ) : (
                    <>
                      <ListView.Title>{item.name}</ListView.Title>
                      {item.description ? (
                        <ListView.Description>
                          {item.description}
                        </ListView.Description>
                      ) : null}
                    </>
                  )}
                </ListView.ItemContent>
                {editingId !== item.id && (
                  <ListView.ItemAction>
                    <div className="flex gap-2">
                      <Button className={actionRoleClassName("edit")}
                        variant="ghost"
                        onPress={() => setEditingId(item.id)}
                      >
                        {t("common.actions.edit")}
                      </Button>
                      <Button
                        variant="ghost"
                        className={`${actionRoleClassName("remove")} text-danger`}
                        onPress={() => onDelete(item.id)}
                        isDisabled={isDeleting}
                      >
                        {t("common.actions.delete")}
                      </Button>
                    </div>
                  </ListView.ItemAction>
                )}
              </ListView.Item>
            ))}
          </ListView>
        )}
      </div>
    </Surface>
  );
}

// Services section
function ServicesSection({
  items,
  serviceTypes,
  isCreating,
  isUpdating,
  isDeleting,
  onCreate,
  onUpdate,
  onDelete,
  onNavigate,
}: {
  items: readonly {
    id: string;
    name: string;
    serviceTypeName?: string | null;
    serviceTypeId: string;
  }[];
  serviceTypes: readonly { id: string; name: string }[];
  isCreating: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
  onCreate: (name: string, description: string, serviceTypeId: string) => void;
  onUpdate: (
    id: string,
    name: string,
    description: string,
    serviceTypeId: string,
  ) => void;
  onDelete: (id: string) => void;
  onNavigate: (intent: NavigationIntent) => void;
}) {
  const { t } = useLocalizedText();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  const handleCreate = useCallback(
    (name: string, description: string, serviceTypeId: string) => {
      onCreate(name, description, serviceTypeId);
      setCreating(false);
    },
    [onCreate],
  );

  const handleUpdate = useCallback(
    (id: string, name: string, description: string, serviceTypeId: string) => {
      onUpdate(id, name, description, serviceTypeId);
      setEditingId(null);
    },
    [onUpdate],
  );

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="briefcase-medical" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.services.title")}</h2>
          </div>
        </div>
        <Button className={actionRoleClassName("create")}
          variant="primary"
          onPress={() => setCreating(true)}
          isDisabled={creating || isCreating}
        >
          {t("common.actions.new")}
        </Button>
      </div>

      <div className="flex flex-col gap-4">
        {creating && (
          <ServiceEditor
            initialValue={{
              name: "",
              description: "",
              serviceTypeId: serviceTypes[0]?.id ?? "",
            }}
            serviceTypes={serviceTypes}
            isPending={isCreating}
            onSubmit={(value) =>
              handleCreate(value.name, value.description, value.serviceTypeId)
            }
            onCancel={() => setCreating(false)}
          />
        )}

        {items.length === 0 && !creating ? (
          <ClinicEmptyState title={t("clinic.services.empty")} />
        ) : (
          <ListView aria-label={t("clinic.services.title")} variant="secondary">
            {items.map((item) => (
              <ListView.Item key={item.id} id={item.id} textValue={item.name}>
                <ListView.ItemContent>
                  {editingId === item.id ? (
                    <ServiceEditor
                      initialValue={{
                        name: item.name,
                        description: "",
                        serviceTypeId: item.serviceTypeId,
                      }}
                      serviceTypes={serviceTypes}
                      isPending={isUpdating}
                      onSubmit={(value) =>
                        handleUpdate(
                          item.id,
                          value.name,
                          value.description,
                          value.serviceTypeId,
                        )
                      }
                      onCancel={() => setEditingId(null)}
                    />
                  ) : (
                    <>
                      <ListView.Title>{item.name}</ListView.Title>
                      <ListView.Description>
                        {item.serviceTypeName ?? t("clinic.services.noType")}
                      </ListView.Description>
                    </>
                  )}
                </ListView.ItemContent>
                {editingId !== item.id && (
                  <ListView.ItemAction>
                    <div className="flex gap-2">
                      <Button className={actionRoleClassName("edit")}
                        variant="ghost"
                        onPress={() => setEditingId(item.id)}
                      >
                        {t("common.actions.edit")}
                      </Button>
                      <Button
                        variant="ghost"
                        className={`${actionRoleClassName("remove")} text-danger`}
                        onPress={() => onDelete(item.id)}
                        isDisabled={isDeleting}
                      >
                        {t("common.actions.delete")}
                      </Button>
                    </div>
                  </ListView.ItemAction>
                )}
              </ListView.Item>
            ))}
          </ListView>
        )}
      </div>
    </Surface>
  );
}

// Providers section
function ProvidersSection({
  items,
  services,
  isCreating,
  isUpdating,
  isDeleting,
  onCreate,
  onUpdate,
  onDelete,
  onNavigate,
}: {
  items: readonly {
    id: string;
    name: string;
    linkedUserIds: readonly string[];
  }[];
  services: readonly { id: string; name: string }[];
  isCreating: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
  onCreate: (name: string, description: string) => void;
  onUpdate: (id: string, name: string, description: string) => void;
  onDelete: (id: string) => void;
  onNavigate: (intent: NavigationIntent) => void;
}) {
  const { t } = useLocalizedText();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

  const handleCreate = useCallback(
    (name: string, description: string) => {
      onCreate(name, description);
      setCreating(false);
    },
    [onCreate],
  );

  const handleUpdate = useCallback(
    (id: string, name: string, description: string) => {
      onUpdate(id, name, description);
      setEditingId(null);
    },
    [onUpdate],
  );

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="user-doctor" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.serviceProviders.title")}</h2>
          </div>
        </div>
        <Button className={actionRoleClassName("create")}
          variant="primary"
          onPress={() => setCreating(true)}
          isDisabled={creating || isCreating}
        >
          {t("common.actions.new")}
        </Button>
      </div>

      <div className="flex flex-col gap-4">
        {creating && (
          <InlineEditor
            initialValue={{ name: "", description: "" }}
            isPending={isCreating}
            onSubmit={(value) => handleCreate(value.name, value.description)}
            onCancel={() => setCreating(false)}
          />
        )}

        {items.length === 0 && !creating ? (
          <ClinicEmptyState title={t("clinic.serviceProviders.empty")} />
        ) : (
          <ListView aria-label={t("clinic.serviceProviders.title")} variant="secondary">
            {items.map((item) => (
              <ListView.Item key={item.id} id={item.id} textValue={item.name}>
                <ListView.ItemContent>
                  {editingId === item.id ? (
                    <InlineEditor
                      initialValue={{ name: item.name, description: "" }}
                      isPending={isUpdating}
                      onSubmit={(value) =>
                        handleUpdate(item.id, value.name, value.description)
                      }
                      onCancel={() => setEditingId(null)}
                    />
                  ) : (
                    <>
                      <ListView.Title>{item.name}</ListView.Title>
                      <ListView.Description>
                        {t("clinic.serviceProviders.linkedUsers", {
                          count: item.linkedUserIds.length,
                        })}
                      </ListView.Description>
                    </>
                  )}
                </ListView.ItemContent>
                {editingId !== item.id && (
                  <ListView.ItemAction>
                    <div className="flex gap-2">
                      <Button className={actionRoleClassName("edit")}
                        variant="ghost"
                        onPress={() => setEditingId(item.id)}
                      >
                        {t("common.actions.edit")}
                      </Button>
                      <Button
                        variant="ghost"
                        className={`${actionRoleClassName("remove")} text-danger`}
                        onPress={() => onDelete(item.id)}
                        isDisabled={isDeleting}
                      >
                        {t("common.actions.delete")}
                      </Button>
                    </div>
                  </ListView.ItemAction>
                )}
              </ListView.Item>
            ))}
          </ListView>
        )}
      </div>
    </Surface>
  );
}

// Shifts section
function ShiftsSection({
  items,
  providers,
  services,
  isCreating,
  isDeleting,
  onCreate,
  onDelete,
  onNavigate,
}: {
  items: readonly {
    id: string;
    dayOfWeek: number;
    startTime: string;
    endTime: string;
    totalSeats: number;
    serviceProviderId?: string | null;
    serviceId?: string | null;
  }[];
  providers: readonly { id: string; name: string }[];
  services: readonly { id: string; name: string }[];
  isCreating: boolean;
  isDeleting: boolean;
  onCreate: (shift: {
    dayOfWeek: number;
    startTime: string;
    endTime: string;
    totalSeats: number;
    timeZone: string;
    serviceProviderId?: string;
    serviceId?: string;
  }) => void;
  onDelete: (id: string) => void;
  onNavigate: (intent: NavigationIntent) => void;
}) {
  const { t } = useLocalizedText();
  const [creating, setCreating] = useState(false);

  const dayNameKeys = [
    "clinic.weekdays.sunday",
    "clinic.weekdays.monday",
    "clinic.weekdays.tuesday",
    "clinic.weekdays.wednesday",
    "clinic.weekdays.thursday",
    "clinic.weekdays.friday",
    "clinic.weekdays.saturday",
  ] as const;

  const handleCreate = useCallback(
    (shift: {
      dayOfWeek: number;
      startTime: string;
      endTime: string;
      totalSeats: number;
      timeZone: string;
      serviceProviderId?: string;
      serviceId?: string;
    }) => {
      onCreate(shift);
      setCreating(false);
    },
    [onCreate],
  );

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="clock" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.appointments.shifts.title")}</h2>
          </div>
        </div>
        <Button className={actionRoleClassName("create")}
          variant="primary"
          onPress={() => setCreating(true)}
          isDisabled={creating || isCreating}
        >
          {t("clinic.appointments.shifts.new")}
        </Button>
      </div>

      <div className="flex flex-col gap-4">
        {creating && (
          <ShiftEditor
            providers={providers}
            services={services}
            isPending={isCreating}
            onSubmit={handleCreate}
            onCancel={() => setCreating(false)}
          />
        )}

        {items.length === 0 && !creating ? (
          <ClinicEmptyState title={t("clinic.appointments.shifts.empty")} />
        ) : (
          <ListView aria-label={t("clinic.appointments.shifts.title")} variant="secondary">
            {items.map((item) => (
              <ListView.Item
                key={item.id}
                id={item.id}
                textValue={`${t(dayNameKeys[item.dayOfWeek])} ${item.startTime}`}
              >
                <ListView.ItemContent>
                  <div className="flex flex-col gap-1">
                    <ListView.Title>
                      {t(dayNameKeys[item.dayOfWeek])} {item.startTime} -{" "}
                      {item.endTime}
                    </ListView.Title>
                    <ListView.Description>
                      {t("clinic.fields.totalSeats")}: {item.totalSeats}
                      {item.serviceProviderId ? (
                        <span className="ml-2">
                          ·{" "}
                          {providers.find((p) => p.id === item.serviceProviderId)
                            ?.name ?? ""}
                        </span>
                      ) : null}
                    </ListView.Description>
                  </div>
                </ListView.ItemContent>
                <ListView.ItemAction>
                  <div className="flex gap-2">
                    <span className="text-xs text-muted">
                      {item.serviceId
                        ? t("clinic.appointments.shifts.linked")
                        : t("clinic.appointments.shifts.general")}
                    </span>
                    <Button
                      variant="ghost"
                      className={`${actionRoleClassName("remove")} text-danger`}
                      onPress={() => onDelete(item.id)}
                      isDisabled={isDeleting}
                    >
                      {t("common.actions.delete")}
                    </Button>
                  </div>
                </ListView.ItemAction>
              </ListView.Item>
            ))}
          </ListView>
        )}
      </div>
    </Surface>
  );
}

// Inline editor for simple name/description entities
function InlineEditor({
  initialValue,
  isPending,
  onSubmit,
  onCancel,
}: {
  initialValue: { name: string; description: string };
  isPending: boolean;
  onSubmit: (value: { name: string; description: string }) => void;
  onCancel: () => void;
}) {
  const { t } = useLocalizedText();
  const [name, setName] = useState(initialValue.name);
  const [description, setDescription] = useState(initialValue.description);

  return (
    <Surface className="rounded-lg p-4" variant="secondary">
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit({ name, description });
        }}
      >
        <TextField isRequired>
          <Label>{t("clinic.fields.name")}</Label>
          <Input value={name} onChange={(event) => setName(event.target.value)} />
        </TextField>
        <TextField>
          <Label>{t("clinic.fields.description")}</Label>
          <Input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </TextField>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onCancel} isDisabled={isPending}>
            {t("common.actions.cancel")}
          </Button>
          <Button variant="primary" type="submit" isPending={isPending}>
            {t("common.actions.save")}
          </Button>
        </div>
      </form>
    </Surface>
  );
}

// Service editor with type selector
function ServiceEditor({
  initialValue,
  serviceTypes,
  isPending,
  onSubmit,
  onCancel,
}: {
  initialValue: { name: string; description: string; serviceTypeId: string };
  serviceTypes: readonly { id: string; name: string }[];
  isPending: boolean;
  onSubmit: (value: {
    name: string;
    description: string;
    serviceTypeId: string;
  }) => void;
  onCancel: () => void;
}) {
  const { t } = useLocalizedText();
  const [name, setName] = useState(initialValue.name);
  const [description, setDescription] = useState(initialValue.description);
  const [serviceTypeId, setServiceTypeId] = useState(
    initialValue.serviceTypeId,
  );

  return (
    <Surface className="rounded-lg p-4" variant="secondary">
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit({ name, description, serviceTypeId });
        }}
      >
        <TextField isRequired>
          <Label>{t("clinic.fields.name")}</Label>
          <Input value={name} onChange={(event) => setName(event.target.value)} />
        </TextField>
        <TextField>
          <Label>{t("clinic.fields.description")}</Label>
          <Input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </TextField>
        <div className="flex flex-col gap-1">
          <Label>{t("clinic.fields.serviceType")}</Label>
          <select
            value={serviceTypeId}
            onChange={(event) => setServiceTypeId(event.target.value)}
            className="rounded-md border border-border bg-surface px-3 py-2 text-sm"
          >
            {serviceTypes.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </select>
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onCancel} isDisabled={isPending}>
            {t("common.actions.cancel")}
          </Button>
          <Button variant="primary" type="submit" isPending={isPending}>
            {t("common.actions.save")}
          </Button>
        </div>
      </form>
    </Surface>
  );
}

// Shift editor
function ShiftEditor({
  providers,
  services,
  isPending,
  onSubmit,
  onCancel,
}: {
  providers: readonly { id: string; name: string }[];
  services: readonly { id: string; name: string }[];
  isPending: boolean;
  onSubmit: (shift: {
    dayOfWeek: number;
    startTime: string;
    endTime: string;
    totalSeats: number;
    timeZone: string;
    serviceProviderId?: string;
    serviceId?: string;
  }) => void;
  onCancel: () => void;
}) {
  const { t } = useLocalizedText();
  const [dayOfWeek, setDayOfWeek] = useState(0);
  const [startTime, setStartTime] = useState("09:00");
  const [endTime, setEndTime] = useState("17:00");
  const [totalSeats, setTotalSeats] = useState(10);
  const [serviceProviderId, setServiceProviderId] = useState<string>("");
  const [serviceId, setServiceId] = useState<string>("");
  const timeZone = getLocalTimeZone();

  const dayNameKeys = [
    "clinic.weekdays.sunday",
    "clinic.weekdays.monday",
    "clinic.weekdays.tuesday",
    "clinic.weekdays.wednesday",
    "clinic.weekdays.thursday",
    "clinic.weekdays.friday",
    "clinic.weekdays.saturday",
  ] as const;

  return (
    <Surface className="rounded-lg p-4" variant="secondary">
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit({
            dayOfWeek,
            startTime,
            endTime,
            totalSeats,
            timeZone,
            serviceProviderId: serviceProviderId || undefined,
            serviceId: serviceId || undefined,
          });
        }}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1">
            <Label>{t("clinic.fields.dayOfWeek")}</Label>
            <select
              value={dayOfWeek}
              onChange={(event) => setDayOfWeek(Number(event.target.value))}
              className="rounded-md border border-border bg-surface px-3 py-2 text-sm"
            >
              {dayNameKeys.map((key, index) => (
                <option key={index} value={index}>
                  {t(key)}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1">
            <Label>{t("clinic.fields.totalSeats")}</Label>
            <Input
              type="number"
              value={totalSeats}
              onChange={(event) => setTotalSeats(Number(event.target.value))}
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label>{t("clinic.fields.startTime")}</Label>
            <Input
              type="time"
              value={startTime}
              onChange={(event) => setStartTime(event.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label>{t("clinic.fields.endTime")}</Label>
            <Input
              type="time"
              value={endTime}
              onChange={(event) => setEndTime(event.target.value)}
            />
          </div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1">
            <Label>{t("clinic.serviceProviders.title")}</Label>
            <select
              value={serviceProviderId}
              onChange={(event) => setServiceProviderId(event.target.value)}
              className="rounded-md border border-border bg-surface px-3 py-2 text-sm"
            >
              <option value="">{t("clinic.appointments.shifts.general")}</option>
              {providers.map((provider) => (
                <option key={provider.id} value={provider.id}>
                  {provider.name}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1">
            <Label>{t("clinic.services.title")}</Label>
            <select
              value={serviceId}
              onChange={(event) => setServiceId(event.target.value)}
              className="rounded-md border border-border bg-surface px-3 py-2 text-sm"
            >
              <option value="">{t("clinic.appointments.shifts.general")}</option>
              {services.map((service) => (
                <option key={service.id} value={service.id}>
                  {service.name}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onCancel} isDisabled={isPending}>
            {t("common.actions.cancel")}
          </Button>
          <Button variant="primary" type="submit" isPending={isPending}>
            {t("common.actions.save")}
          </Button>
        </div>
      </form>
    </Surface>
  );
}
