"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Badge from "@/components/ui/badge/Badge";
import CreateButton from "@/components/common/CreateButton";
import DataTable, { DataTableColumn } from "@/components/common/DataTable";
import { ConfirmModal } from "@/components/ui/modal";
import { useToast } from "@/context/ToastContext";
import { PencilIcon, TrashBinIcon } from "@/icons";
import { useProcesses } from "@/hooks/useProcesses";
import ProcessService, { Process } from "@/services/ProcessService";
import ProcessModal from "./ProcessModal";

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

const baseColumns: DataTableColumn<Process>[] = [
  {
    key: "code",
    header: "Code",
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
    key: "parameters",
    header: "Parameters",
    className: "min-w-80",
    render: (_, row) => {
      const parameters = row.parameters ?? [];
      const visibleParameters = parameters.slice(0, 3);
      const hiddenCount = parameters.length - visibleParameters.length;

      if (parameters.length === 0) {
        return "-";
      }

      return (
        <div className="flex flex-wrap gap-2">
          {visibleParameters.map((parameter) => (
            <span
              key={parameter.id}
              className="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-700 dark:bg-white/[0.06] dark:text-gray-300"
              title={parameter.name}
            >
              {parameter.code}
            </span>
          ))}
          {hiddenCount > 0 && (
            <span className="rounded-full bg-brand-50 px-2.5 py-1 text-xs font-medium text-brand-700 dark:bg-brand-500/10 dark:text-brand-300">
              +{hiddenCount}
            </span>
          )}
        </div>
      );
    },
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

export default function ProcessTable() {
  const toast = useToast();
  const lastErrorRef = useRef<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedProcess, setSelectedProcess] = useState<Process | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [processToDelete, setProcessToDelete] = useState<Process | null>(null);
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
  } = useProcesses({
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
      title: "Failed to load processes",
    });
  }, [error, toast]);

  const handleCreate = () => {
    setSelectedProcess(null);
    setIsModalOpen(true);
  };

  const handleUpdate = useCallback((process: Process) => {
    setSelectedProcess(process);
    setIsModalOpen(true);
  }, []);

  const handleDeleteClick = useCallback((process: Process) => {
    setProcessToDelete(process);
    setIsDeleteModalOpen(true);
  }, []);

  const closeDeleteModal = () => {
    if (!isDeleting) {
      setIsDeleteModalOpen(false);
    }
  };

  const confirmDelete = async () => {
    if (!processToDelete) {
      return;
    }

    setIsDeleting(true);
    try {
      await ProcessService.deleteProcess(processToDelete.id);
      toast.success({
        title: "Success",
        message: "Process deleted successfully",
      });
      refetch();
      setIsDeleteModalOpen(false);
      setProcessToDelete(null);
    } catch (error: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(error, "Failed to delete process"),
      });
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<DataTableColumn<Process>[]>(
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
        emptyMessage="No processes found"
        error={error}
        isLoading={isLoading}
        minWidth="1120px"
        onLimitChange={setLimit}
        onPageChange={setPage}
        onSearchChange={setSearch}
        pagination={pagination}
        rowKey="id"
        searchPlaceholder="Search processes"
        searchValue={query.search}
      />

      <ProcessModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={refetch}
        process={selectedProcess}
      />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={closeDeleteModal}
        onConfirm={confirmDelete}
        title="Delete Process"
        message={`Are you sure you want to delete process "${processToDelete?.name}"?`}
        confirmText="Delete"
        isDestructive={true}
        isLoading={isDeleting}
      />
    </>
  );
}
