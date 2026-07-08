"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Badge from "@/components/ui/badge/Badge";
import CreateButton from "@/components/common/CreateButton";
import DataTable, { DataTableColumn } from "@/components/common/DataTable";
import { useParts } from "@/hooks/useParts";
import PartService, { Part } from "@/services/PartService";
import { useToast } from "@/context/ToastContext";
import { PencilIcon, TrashBinIcon } from "@/icons";
import { ConfirmModal } from "@/components/ui/modal";
import PartModal from "./PartModal";

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

const baseColumns: DataTableColumn<Part>[] = [
  {
    key: "number",
    header: "Number",
    sortable: true,
  },
  {
    key: "name",
    header: "Name",
    sortable: true,
  },
  {
    key: "description",
    header: "Description",
    className: "min-w-72",
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

export default function PartTable() {
  const toast = useToast();
  const lastErrorRef = useRef<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedPart, setSelectedPart] = useState<Part | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [partToDelete, setPartToDelete] = useState<Part | null>(null);
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
  } = useParts({
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
      title: "Failed to load parts",
    });
  }, [error, toast]);

  const handleCreate = () => {
    setSelectedPart(null);
    setIsModalOpen(true);
  };

  const handleUpdate = useCallback((part: Part) => {
    setSelectedPart(part);
    setIsModalOpen(true);
  }, []);

  const handleDeleteClick = useCallback((part: Part) => {
    setPartToDelete(part);
    setIsDeleteModalOpen(true);
  }, []);

  const closeDeleteModal = () => {
    if (!isDeleting) {
      setIsDeleteModalOpen(false);
    }
  };

  const confirmDelete = async () => {
    if (!partToDelete) {
      return;
    }

    setIsDeleting(true);
    try {
      await PartService.deletePart(partToDelete.id);
      toast.success({
        title: "Success",
        message: "Part deleted successfully",
      });
      refetch();
      setIsDeleteModalOpen(false);
      setPartToDelete(null);
    } catch (error: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(error, "Failed to delete part"),
      });
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<DataTableColumn<Part>[]>(
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
          <div className="flex items-center gap-3">
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

            <CreateButton onClick={handleCreate} />
          </div>
        }
        columns={columns}
        data={data}
        emptyMessage="No parts found"
        error={error}
        isLoading={isLoading}
        minWidth="920px"
        onLimitChange={setLimit}
        onPageChange={setPage}
        onSearchChange={setSearch}
        pagination={pagination}
        rowKey="id"
        searchPlaceholder="Search parts"
        searchValue={query.search}
      />

      <PartModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={refetch}
        part={selectedPart}
      />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={closeDeleteModal}
        onConfirm={confirmDelete}
        title="Delete Part"
        message={`Are you sure you want to delete part "${partToDelete?.name}"?`}
        confirmText="Delete"
        isDestructive={true}
        isLoading={isDeleting}
      />
    </>
  );
}
