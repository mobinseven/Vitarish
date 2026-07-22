"use client";

import { useState } from "react";
import { Button, Label, Input, TextField } from "@heroui/react";
import { DataGrid, type DataGridColumn, Widget } from "@heroui-pro/react";
import { SeaLoadingState, SeaEmptyState } from "../state/SeaStateSurface";
import { useSeaRoles } from "../../hooks/roles/useSeaRoles";
import {
  useCreateSeaRole,
  useDeleteSeaRole,
  useUpdateSeaRole,
} from "../../hooks/roles/useSeaRoleMutations";
import { useLocalizedText } from "../../localization";
import type { SeaRoleReadDto } from "../../api/roles/seaRolesApi";
import { SeaUserRoleAssignmentsPanel } from "./SeaUserRoleAssignmentsPanel";

export function SeaRolesPage() {
  const { t } = useLocalizedText();
  const { data: roles, isLoading } = useSeaRoles();
  const createMutation = useCreateSeaRole();
  const updateMutation = useUpdateSeaRole();
  const deleteMutation = useDeleteSeaRole();

  const [isAdding, setIsAdding] = useState(false);
  const [newRoleName, setNewRoleName] = useState("");
  const [editTarget, setEditTarget] = useState<SeaRoleReadDto | null>(null);
  const [editRoleName, setEditRoleName] = useState("");
  const [deleteTarget, setDeleteTarget] = useState<SeaRoleReadDto | null>(null);

  const beginCreateRole = () => {
    setEditTarget(null);
    setEditRoleName("");
    setDeleteTarget(null);
    setIsAdding(true);
  };

  const beginEditRole = (role: SeaRoleReadDto) => {
    setIsAdding(false);
    setNewRoleName("");
    setDeleteTarget(null);
    setEditTarget(role);
    setEditRoleName(role.name);
  };

  const handleCreateRole = () => {
    if (!newRoleName.trim()) return;
    createMutation.mutate(
      { name: newRoleName.trim() },
      {
        onSuccess: () => {
          setNewRoleName("");
          setIsAdding(false);
        },
      },
    );
  };

  const handleUpdateRole = () => {
    if (!editTarget || !editRoleName.trim()) return;

    updateMutation.mutate(
      { id: editTarget.id, name: editRoleName.trim() },
      {
        onSuccess: () => {
          setEditTarget(null);
          setEditRoleName("");
        },
      },
    );
  };

  const handleDeleteRole = () => {
    if (!deleteTarget) return;

    deleteMutation.mutate(deleteTarget.id, {
      onSuccess: () => setDeleteTarget(null),
    });
  };

  const columns: DataGridColumn<SeaRoleReadDto>[] = [
    {
      id: "name",
      header: t("sea.roles.roleName"),
      accessorKey: "name",
      isRowHeader: true,
      minWidth: 200,
    },
    {
      id: "actions",
      header: "",
      minWidth: 180,
      cell: (item) => (
        <div className="flex gap-2">
          <Button
            size="sm"
            variant="ghost"
            onPress={() => beginEditRole(item)}
            isDisabled={updateMutation.isPending || deleteMutation.isPending}
          >
            {t("common.actions.edit")}
          </Button>
          <Button
            size="sm"
            variant="danger"
            onPress={() => {
              setEditTarget(null);
              setEditRoleName("");
              setIsAdding(false);
              setDeleteTarget(item);
            }}
            isDisabled={deleteMutation.isPending}
          >
            {t("sea.roles.deleteRole")}
          </Button>
        </div>
      ),
    },
  ];

  if (isLoading) {
    return <SeaLoadingState label={t("sea.roles.loading")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("sea.roles.title")}</h1>
        <Button variant="primary" onPress={beginCreateRole}>
          {t("sea.roles.createRole")}
        </Button>
      </div>

      {isAdding && (
        <Widget>
          <Widget.Content className="flex flex-col gap-4">
            <TextField isRequired value={newRoleName} onChange={setNewRoleName}>
              <Label>{t("sea.roles.roleName")}</Label>
              <Input placeholder={t("sea.roles.roleNamePlaceholder")} />
            </TextField>
            <div className="flex justify-end gap-2">
              <Button
                variant="ghost"
                onPress={() => {
                  setIsAdding(false);
                  setNewRoleName("");
                }}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                variant="primary"
                onPress={handleCreateRole}
                isDisabled={!newRoleName.trim() || createMutation.isPending}
              >
                {t("common.actions.save")}
              </Button>
            </div>
          </Widget.Content>
        </Widget>
      )}

      {editTarget && (
        <Widget>
          <Widget.Content className="flex flex-col gap-4">
            <TextField
              isRequired
              value={editRoleName}
              onChange={setEditRoleName}
            >
              <Label>{t("sea.roles.roleName")}</Label>
              <Input placeholder={t("sea.roles.roleNamePlaceholder")} />
            </TextField>
            <div className="flex justify-end gap-2">
              <Button
                variant="ghost"
                onPress={() => {
                  setEditTarget(null);
                  setEditRoleName("");
                }}
                isDisabled={updateMutation.isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                variant="primary"
                onPress={handleUpdateRole}
                isDisabled={!editRoleName.trim() || updateMutation.isPending}
              >
                {updateMutation.isPending
                  ? t("common.states.saving")
                  : t("common.actions.save")}
              </Button>
            </div>
          </Widget.Content>
        </Widget>
      )}

      {(roles ?? []).length === 0 && !isAdding ? (
        <SeaEmptyState title={t("sea.roles.emptyState")} />
      ) : (
        <DataGrid
          aria-label={t("sea.roles.title")}
          columns={columns}
          data={roles ?? []}
          getRowId={(item) => item.id}
          variant="primary"
        />
      )}

      <SeaUserRoleAssignmentsPanel roles={roles ?? []} />

      {deleteTarget && (
        <div className="rounded-lg border border-danger/20 bg-danger/5 p-4">
          <p className="text-sm mb-2">{t("sea.roles.confirmDelete")}</p>
          <p className="text-sm font-medium mb-3">{deleteTarget.name}</p>
          <div className="flex gap-2">
            <Button
              size="sm"
              variant="ghost"
              onPress={() => setDeleteTarget(null)}
              isDisabled={deleteMutation.isPending}
            >
              {t("common.actions.cancel")}
            </Button>
            <Button
              size="sm"
              variant="danger"
              onPress={handleDeleteRole}
              isDisabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending
                ? t("common.states.saving")
                : t("common.actions.delete")}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
