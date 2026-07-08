"use client";

import { useEffect, useRef, useMemo, useState } from "react";
import Badge from "@/components/ui/badge/Badge";
import CreateButton from "@/components/common/CreateButton";
import DataTable, { DataTableColumn } from "@/components/common/DataTable";
import { useParameters } from "@/hooks/useParameters";
import ParameterService, { Parameter } from "@/services/ParameterService";
import { useToast } from "@/context/ToastContext";
import { PencilIcon, TrashBinIcon } from "@/icons";
import ParameterModal from "./ParameterModal";
import { ConfirmModal } from "@/components/ui/modal";

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

const baseColumns: DataTableColumn<Parameter>[] = [
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
    key: "dataType",
    header: "Data Type",
    render: (value) => (
      <span className="capitalize">{typeof value === "string" ? value : "-"}</span>
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

export default function ParameterTable() {
  const toast = useToast();
  const lastErrorRef = useRef<string | null>(null);
  
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedParameter, setSelectedParameter] = useState<Parameter | null>(null);
  
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [parameterToDelete, setParameterToDelete] = useState<Parameter | null>(null);
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
  } = useParameters({
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
      title: "Failed to load parameters",
    });
  }, [error, toast]);

  const handleCreate = () => {
    setSelectedParameter(null);
    setIsModalOpen(true);
  };

  const handleUpdate = (parameter: Parameter) => {
    setSelectedParameter(parameter);
    setIsModalOpen(true);
  };

  const handleDeleteClick = (parameter: Parameter) => {
    setParameterToDelete(parameter);
    setIsDeleteModalOpen(true);
  };

  const confirmDelete = async () => {
    if (!parameterToDelete) return;
    
    setIsDeleting(true);
    try {
      await ParameterService.deleteParameter(parameterToDelete.id);
      toast.success({
        title: "Success",
        message: "Parameter deleted successfully",
      });
      refetch();
      setIsDeleteModalOpen(false);
      setParameterToDelete(null);
    } catch (err: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(err, "Failed to delete parameter"),
      });
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<DataTableColumn<Parameter>[]>(() => [
    ...baseColumns,
    {
      key: "action",
      header: "Action",
      align: "center",
      render: (_, row) => (
        <div className="flex items-center justify-center gap-3">
          <button
            onClick={() => handleUpdate(row)}
            className="text-warning-500 hover:text-warning-600 dark:text-warning-400 dark:hover:text-warning-500 transition-colors"
            title="Edit"
          >
            <PencilIcon className="w-5 h-5 fill-current" />
          </button>
          <button
            onClick={() => handleDeleteClick(row)}
            className="text-error-500 hover:text-error-600 dark:text-error-400 dark:hover:text-error-500 transition-colors"
            title="Delete"
          >
            <TrashBinIcon className="w-5 h-5 fill-current" />
          </button>
        </div>
      ),
    },
  ], []);

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
        emptyMessage="No parameters found"
        error={error}
        isLoading={isLoading}
        minWidth="980px"
        onLimitChange={setLimit}
        onPageChange={setPage}
        onSearchChange={setSearch}
        pagination={pagination}
        rowKey="id"
        searchPlaceholder="Search parameters"
        searchValue={query.search}
      />

      <ParameterModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={refetch}
        parameter={selectedParameter}
      />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={() => !isDeleting && setIsDeleteModalOpen(false)}
        onConfirm={confirmDelete}
        title="Delete Parameter"
        message={`Are you sure you want to delete parameter "${parameterToDelete?.name}"?`}
        confirmText="Delete"
        isDestructive={true}
        isLoading={isDeleting}
      />
    </>
  );
}
