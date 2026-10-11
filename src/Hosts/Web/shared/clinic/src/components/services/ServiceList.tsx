import React from "react";
import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState, removeButtonClassName } from "@vitarish/ui";
import { useLocalizedText } from "../../localization/index";

export interface ServiceListItem {
  id: string;
  name: string;
  serviceTypeName?: string | null;
}

export interface ServiceListProps {
  items: readonly ServiceListItem[];
  onDelete: (serviceId: string) => void;
}

export function ServiceList({ items, onDelete }: ServiceListProps) {
  const { t } = useLocalizedText();

  return (
    <ListView
      aria-label={t("clinic.services.title")}
      renderEmptyState={() => (
        <ClinicEmptyState title={t("clinic.services.empty")} />
      )}
    >
      {items.map((service) => (
        <ListView.Item
          key={service.id}
          id={service.id}
          textValue={service.name}
        >
          <ListView.ItemContent>
            <div className="flex flex-col gap-1">
              <ListView.Title>{service.name}</ListView.Title>
              <ListView.Description>
                {service.serviceTypeName ?? t("clinic.services.noType")}
              </ListView.Description>
            </div>
          </ListView.ItemContent>
          <ListView.ItemAction>
            <Button
              variant="ghost"
              className={`${removeButtonClassName} text-danger`}
              onPress={() => onDelete(service.id)}
            >
              {t("common.actions.delete")}
            </Button>
          </ListView.ItemAction>
        </ListView.Item>
      ))}
    </ListView>
  );
}
