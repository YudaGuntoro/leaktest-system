"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Badge from "@/components/ui/badge/Badge";
import CreateButton from "@/components/common/CreateButton";
import DataTable, { DataTableColumn } from "@/components/common/DataTable";
import { ConfirmModal } from "@/components/ui/modal";
import { useToast } from "@/context/ToastContext";
import { PencilIcon, TrashBinIcon } from "@/icons";
import { useUsers } from "@/hooks/useUsers";
import UserService, { User } from "@/services/UserService";
import UserModal from "./UserModal";

const roleOptions = [
  { label: "Admin", value: "admin" },
  { label: "User", value: "user" },
];

const dateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  month: "short",
  year: "numeric",
});

const formatDate = (value: string) => {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return dateFormatter.format(date);
};

const getErrorMessage = (error: unknown, fallback: string) =>
  error instanceof Error ? error.message : fallback;

const getInitials = (name: string) => {
  const initials = name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join("");

  return initials.toUpperCase() || "U";
};

const formatRole = (role: string) => {
  const option = roleOptions.find((item) => item.value === role);

  if (option) {
    return option.label;
  }

  return role || "-";
};

const baseColumns: DataTableColumn<User>[] = [
  {
    key: "name",
    header: "User",
    className: "min-w-64",
    sortable: true,
    render: (_, row) => (
      <div className="flex items-center gap-3">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-brand-50 text-sm font-semibold text-brand-600 dark:bg-brand-500/15 dark:text-brand-300">
          {getInitials(row.name || row.username)}
        </div>
        <div>
          <div className="font-medium text-gray-800 dark:text-white/90">
            {row.name || "-"}
          </div>
          <div className="text-theme-xs text-gray-500 dark:text-gray-400">
            @{row.username}
          </div>
        </div>
      </div>
    ),
  },
  {
    key: "role",
    header: "Role",
    render: (value) => (
      <Badge color="info" size="sm">
        {typeof value === "string" ? formatRole(value) : "-"}
      </Badge>
    ),
  },
  {
    key: "isActive",
    header: "Status",
    render: (value) => (
      <Badge color={value ? "success" : "error"} size="sm">
        {value ? "Active" : "Inactive"}
      </Badge>
    ),
  },
  {
    key: "createdAt",
    header: "Created At",
    render: (value) => (typeof value === "string" ? formatDate(value) : "-"),
  },
];

export default function UserTable() {
  const toast = useToast();
  const lastErrorRef = useRef<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [userToDelete, setUserToDelete] = useState<User | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const {
    data,
    error,
    isLoading,
    pagination,
    query,
    refetch,
    setIsActive,
    setLimit,
    setPage,
    setSearch,
  } = useUsers({
    limit: 10,
    page: 1,
  });

  useEffect(() => {
    if (!error || lastErrorRef.current === error) {
      return;
    }

    lastErrorRef.current = error;
    toast.error({
      message: error,
      title: "Failed to load users",
    });
  }, [error, toast]);

  const handleCreate = () => {
    setSelectedUser(null);
    setIsModalOpen(true);
  };

  const handleUpdate = useCallback((user: User) => {
    setSelectedUser(user);
    setIsModalOpen(true);
  }, []);

  const handleDeleteClick = useCallback((user: User) => {
    setUserToDelete(user);
    setIsDeleteModalOpen(true);
  }, []);

  const closeDeleteModal = () => {
    if (!isDeleting) {
      setIsDeleteModalOpen(false);
    }
  };

  const confirmDelete = async () => {
    if (!userToDelete) {
      return;
    }

    setIsDeleting(true);
    try {
      await UserService.deleteUser(userToDelete.id);
      toast.success({
        title: "Success",
        message: "User deleted successfully",
      });
      refetch();
      setIsDeleteModalOpen(false);
      setUserToDelete(null);
    } catch (error: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(error, "Failed to delete user"),
      });
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<DataTableColumn<User>[]>(
    () => [
      ...baseColumns,
      {
        key: "action",
        header: "Action",
        align: "center",
        render: (_, row) => (
          <div className="flex items-center justify-center gap-3">
            <button
              onClick={() => handleUpdate(row)}
              className="text-warning-500 transition-colors hover:text-warning-600 dark:text-warning-400 dark:hover:text-warning-500"
              title="Edit"
              type="button"
            >
              <PencilIcon className="h-5 w-5 fill-current" />
            </button>
            <button
              onClick={() => handleDeleteClick(row)}
              className="text-error-500 transition-colors hover:text-error-600 dark:text-error-400 dark:hover:text-error-500"
              title="Delete"
              type="button"
            >
              <TrashBinIcon className="h-5 w-5 fill-current" />
            </button>
          </div>
        ),
      },
    ],
    [handleDeleteClick, handleUpdate]
  );

  return (
    <>
      <DataTable
        actions={
          <div className="flex flex-wrap items-center gap-3">
            <select
              className="h-10 rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm font-medium text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
              value={
                query.isActive === undefined || query.isActive === null
                  ? "all"
                  : String(query.isActive)
              }
              onChange={(event) => {
                const value = event.target.value;
                setIsActive(value === "all" ? null : value === "true");
              }}
            >
              <option value="all">All Status</option>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>

            <CreateButton onClick={handleCreate}>Create User</CreateButton>
          </div>
        }
        columns={columns}
        data={data}
        emptyMessage="No users found"
        error={error}
        isLoading={isLoading}
        minWidth="840px"
        onLimitChange={setLimit}
        onPageChange={setPage}
        onSearchChange={setSearch}
        pagination={pagination}
        rowKey="id"
        searchPlaceholder="Search users"
        searchValue={query.search}
      />

      <UserModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={refetch}
        user={selectedUser}
      />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={closeDeleteModal}
        onConfirm={confirmDelete}
        title="Delete User"
        message={`Are you sure you want to delete user "${userToDelete?.name}"?`}
        confirmText="Delete"
        isDestructive={true}
        isLoading={isDeleting}
      />
    </>
  );
}
