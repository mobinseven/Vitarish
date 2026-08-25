"use client";

import { useMemo, useState } from "react";
import { Button, Input, Label, TextField } from "@heroui/react";
import { DataGrid, type DataGridColumn, Widget } from "@heroui-pro/react";
import { SeaEmptyState, SeaLoadingState } from "../state/SeaStateSurface";
import { useLocalizedText } from "../../localization";
import type {
  SeaRoleReadDto,
  SeaUserRoleReadDto,
} from "../../api/roles/seaRolesApi";
import type { SeaRoleUserSummary } from "../../api/roles/seaRoleUsersApi";
import { useSeaUserRoles } from "../../hooks/roles/useSeaRoles";
import {
  useAssignSeaUserRole,
  useRemoveSeaUserRole,
} from "../../hooks/roles/useSeaRoleMutations";
import { useSeaRoleUserSearch } from "../../hooks/roles/useSeaRoleUsers";

interface SeaUserRoleAssignmentsPanelProps {
  roles: SeaRoleReadDto[];
}

export function SeaUserRoleAssignmentsPanel({
  roles,
}: SeaUserRoleAssignmentsPanelProps) {
  const { t } = useLocalizedText();
  const [query, setQuery] = useState("");
  const [selectedUser, setSelectedUser] = useState<SeaRoleUserSummary | null>(
    null,
  );
  const [selectedRoleId, setSelectedRoleId] = useState("");
  const [selectedType, setSelectedType] = useState<"Read" | "Write">("Read");
  const [removeTarget, setRemoveTarget] = useState<SeaUserRoleReadDto | null>(
    null,
  );

  const userSearch = useSeaRoleUserSearch(query);
  const userRoles = useSeaUserRoles(selectedUser?.id ?? "");
  const assignMutation = useAssignSeaUserRole();
  const removeMutation = useRemoveSeaUserRole();

  const roleNames = useMemo(
    () => new Map(roles.map((role) => [role.id, role.name])),
    [roles],
  );

  const beginAssignRole = () => {
    if (!selectedUser || !selectedRoleId) return;

    assignMutation.mutate(
      {
        id: selectedUser.id,
        roleId: selectedRoleId,
        type: selectedType,
      },
      {
        onSuccess: () => setSelectedRoleId(""),
      },
    );
  };

  const removeAssignment = () => {
    if (!removeTarget) return;

    removeMutation.mutate(removeTarget.id, {
      onSuccess: () => setRemoveTarget(null),
    });
  };

  const columns: DataGridColumn<SeaUserRoleReadDto>[] = [
    {
      id: "role",
      header: t("sea.roles.roleName"),
      minWidth: 180,
      cell: (item) => roleNames.get(item.roleId) ?? item.roleId,
    },
    {
      id: "type",
      header: t("sea.roles.accessType"),
      minWidth: 120,
      cell: (item) =>
        t(item.type === "Write" ? "sea.roles.write" : "sea.roles.read"),
    },
    {
      id: "actions",
      header: "",
      minWidth: 140,
      cell: (item) => (
        <Button
          size="sm"
          variant="ghost"
          onPress={() => setRemoveTarget(item)}
          isDisabled={removeMutation.isPending}
        >
          {t("sea.roles.removeRole")}
        </Button>
      ),
    },
  ];

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("sea.roles.userAssignments")}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-4">
        <TextField
          value={query}
          onChange={(value) => {
            setQuery(value);
            setSelectedUser(null);
          }}
        >
          <Label>{t("sea.roles.userSearch")}</Label>
          <Input placeholder={t("sea.roles.userSearchPlaceholder")} />
        </TextField>

        {query.trim().length >= 2 && !selectedUser && (
          <div className="flex flex-col gap-2">
            {userSearch.isLoading ? (
              <SeaLoadingState label={t("sea.roles.searchingUsers")} />
            ) : (userSearch.data ?? []).length === 0 ? (
              <SeaEmptyState title={t("sea.roles.noUsersFound")} />
            ) : (
              (userSearch.data ?? []).map((user) => (
                <button
                  key={user.id}
                  type="button"
                  className="rounded-md border border-border px-3 py-2 text-start hover:bg-content2"
                  onClick={() => {
                    setSelectedUser(user);
                    setQuery(
                      user.fullName ?? user.userName ?? user.email ?? user.id,
                    );
                  }}
                >
                  <span className="block text-sm font-medium">
                    {user.fullName ?? user.userName ?? user.id}
                  </span>
                  <span className="block text-xs text-muted">
                    {user.email ?? user.phoneNumber ?? user.id}
                  </span>
                </button>
              ))
            )}
          </div>
        )}

        {selectedUser && (
          <div className="flex flex-col gap-4 rounded-lg border border-border p-3">
            <div>
              <p className="text-sm font-medium">
                {selectedUser.fullName ??
                  selectedUser.userName ??
                  selectedUser.id}
              </p>
              <p className="text-xs text-muted">
                {selectedUser.email ??
                  selectedUser.phoneNumber ??
                  selectedUser.id}
              </p>
            </div>

            <div className="grid gap-3 md:grid-cols-[1fr_auto_auto]">
              <label className="flex flex-col gap-1 text-sm">
                <span>{t("sea.roles.roleName")}</span>
                <select
                  className="h-10 rounded-md border border-border bg-background px-3"
                  value={selectedRoleId}
                  onChange={(event) => setSelectedRoleId(event.target.value)}
                >
                  <option value="">{t("sea.roles.selectRole")}</option>
                  {roles.map((role) => (
                    <option key={role.id} value={role.id}>
                      {role.name}
                    </option>
                  ))}
                </select>
              </label>

              <label className="flex flex-col gap-1 text-sm">
                <span>{t("sea.roles.accessType")}</span>
                <select
                  className="h-10 rounded-md border border-border bg-background px-3"
                  value={selectedType}
                  onChange={(event) =>
                    setSelectedType(
                      event.target.value === "Write" ? "Write" : "Read",
                    )
                  }
                >
                  <option value="Read">{t("sea.roles.read")}</option>
                  <option value="Write">{t("sea.roles.write")}</option>
                </select>
              </label>

              <Button
                className="self-end"
                variant="primary"
                onPress={beginAssignRole}
                isDisabled={!selectedRoleId || assignMutation.isPending}
              >
                {assignMutation.isPending
                  ? t("common.states.saving")
                  : t("sea.roles.assignRole")}
              </Button>
            </div>

            {userRoles.isLoading ? (
              <SeaLoadingState label={t("sea.roles.loadingUserRoles")} />
            ) : (userRoles.data ?? []).length === 0 ? (
              <SeaEmptyState title={t("sea.roles.noUserRoles")} />
            ) : (
              <DataGrid
                aria-label={t("sea.roles.userRoles")}
                columns={columns}
                data={userRoles.data ?? []}
                getRowId={(item) => item.id}
                variant="primary"
              />
            )}
          </div>
        )}

        {removeTarget && (
          <div className="rounded-lg border border-danger/20 bg-danger/5 p-4">
            <p className="mb-3 text-sm">{t("sea.roles.confirmRemove")}</p>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onPress={() => setRemoveTarget(null)}
                isDisabled={removeMutation.isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                size="sm"
                variant="danger"
                onPress={removeAssignment}
                isDisabled={removeMutation.isPending}
              >
                {removeMutation.isPending
                  ? t("common.states.saving")
                  : t("common.actions.delete")}
              </Button>
            </div>
          </div>
        )}
      </Widget.Content>
    </Widget>
  );
}
