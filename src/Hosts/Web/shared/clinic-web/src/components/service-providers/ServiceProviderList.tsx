import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { useLocalizedText } from "../../localization/index";

export interface ServiceProviderListItem {
  id: string;
  name: string;
  linkedUserIds: readonly string[];
}
export interface ServiceProviderListProps {
  items: readonly ServiceProviderListItem[];
  onDelete: (serviceProviderId: string) => void;
}
export function ServiceProviderList({
  items,
  onDelete,
}: ServiceProviderListProps) {
  const { t } = useLocalizedText();
  return (
    <ListView
      aria-label={t("clinic.serviceProviders.title")}
      renderEmptyState={() => (
        <ClinicEmptyState title={t("clinic.serviceProviders.empty")} />
      )}
    >
      {items.map((item) => (
        <ListView.Item key={item.id} id={item.id} textValue={item.name}>
          <ListView.ItemContent>
            <ListView.Title>{item.name}</ListView.Title>
            <ListView.Description>
              {t("clinic.serviceProviders.linkedUsers", {
                count: item.linkedUserIds.length,
              })}
            </ListView.Description>
          </ListView.ItemContent>
          <ListView.ItemAction>
            <Button
              variant="ghost"
              className="text-danger"
              onPress={() => onDelete(item.id)}
            >
              {t("common.actions.delete")}
            </Button>
          </ListView.ItemAction>
        </ListView.Item>
      ))}
    </ListView>
  );
}
