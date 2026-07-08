"use client";

import { useEffect, useRef, useMemo, useState } from "react";
import DataTable, { DataTableColumn } from "@/components/common/DataTable";
import CreateButton from "@/components/common/CreateButton";
import { useStockIns } from "@/hooks/useStockIns";
import StockInService, { StockIn } from "@/services/StockInService";
import { useToast } from "@/context/ToastContext";
import { PencilIcon, TrashBinIcon } from "@/icons";
import { ConfirmModal } from "@/components/ui/modal";
import StockInModal from "./StockInModal";
import DatePicker from "@/components/form/date-picker";

const dateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

const formatDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return dateFormatter.format(date);
};

const getErrorMessage = (error: unknown, fallback: string) =>
  error instanceof Error ? error.message : fallback;

const filterInputClassName =
  "h-10 w-[224px] max-w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90";

export default function StockInTable() {
  const toast = useToast();
  const lastErrorRef = useRef<string | null>(null);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedStockIn, setSelectedStockIn] = useState<StockIn | null>(null);

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [stockInToDelete, setStockInToDelete] = useState<StockIn | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const {
    data,
    error,
    isLoading,
    pagination,
    query,
    refetch,
    setLimit,
    setPage,
    setQuery,
  } = useStockIns({
    limit: 10,
    page: 1,
  });

  useEffect(() => {
    if (!error || lastErrorRef.current === error) return;
    lastErrorRef.current = error;
    toast.error({
      message: error,
      title: "Failed to load stock ins",
    });
  }, [error, toast]);

  const handleCreate = () => {
    setSelectedStockIn(null);
    setIsModalOpen(true);
  };

  const handleUpdate = (stockIn: StockIn) => {
    setSelectedStockIn(stockIn);
    setIsModalOpen(true);
  };

  const handleDeleteClick = (stockIn: StockIn) => {
    setStockInToDelete(stockIn);
    setIsDeleteModalOpen(true);
  };

  const confirmDelete = async () => {
    if (!stockInToDelete) return;
    setIsDeleting(true);
    try {
      await StockInService.deleteStockIn(stockInToDelete.id);
      toast.success({
        title: "Success",
        message: "Stock in record deleted successfully",
      });
      refetch();
      setIsDeleteModalOpen(false);
      setStockInToDelete(null);
    } catch (err: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(err, "Failed to delete stock in record"),
      });
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<DataTableColumn<StockIn>[]>(
    () => [
      {
        key: "code",
        header: "Code",
        sortable: true,
      },
      {
        key: "part.number",
        header: "Part Number",
        render: (_, row) => row.part?.number || "-",
      },
      {
        key: "part.name",
        header: "Part Name",
        render: (_, row) => row.part?.name || "-",
      },
      {
        key: "supplyQty",
        header: "Supply Qty",
        align: "right",
      },
      {
        key: "receiptQty",
        header: "Receipt Qty",
        align: "right",
      },
      {
        key: "supplyDate",
        header: "Supply Date",
        render: (value) => (typeof value === "string" ? formatDate(value) : "-"),
      },
      {
        key: "receiptDate",
        header: "Receipt Date",
        render: (value) => (typeof value === "string" ? formatDate(value) : "-"),
      },
      {
        key: "issues",
        header: "Issues",
        render: (_, row) => (
          <div className="flex flex-wrap gap-1">
            {row.issues.map((issue) => (
              <span
                key={issue.id}
                className="inline-flex items-center rounded bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-800 dark:bg-gray-700 dark:text-gray-300"
              >
                {issue.number}
              </span>
            ))}
            {row.issues.length === 0 && "-"}
          </div>
        ),
      },
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
    ],
    []
  );

  return (
    <>
      <DataTable
        actions={
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center gap-2">
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                Date
              </label>
              <DatePicker
                id="stock-in-date-filter"
                className={`${filterInputClassName} pr-10`}
                defaultDate={query.date}
                onChange={([date]) => {
                  if (date) {
                    const formattedDate = date.toISOString().split("T")[0];
                    setQuery({ date: formattedDate });
                  } else {
                    setQuery({ date: undefined });
                  }
                }}
                placeholder="Select date"
              />
            </div>
            
            <input
              type="text"
              placeholder="Issue Number"
              className={filterInputClassName}
              value={query.issueNumber || ""}
              onChange={(e) => setQuery({ issueNumber: e.target.value })}
            />

            <input
              type="text"
              placeholder="Part Number"
              className={filterInputClassName}
              value={query.partNumber || ""}
              onChange={(e) => setQuery({ partNumber: e.target.value })}
            />

            <CreateButton onClick={handleCreate} />
          </div>
        }
        columns={columns}
        data={data}
        emptyMessage="No stock in records found"
        error={error}
        isLoading={isLoading}
        minWidth="1100px"
        onLimitChange={setLimit}
        onPageChange={setPage}
        pagination={pagination}
        rowKey="id"
      />

      <StockInModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={refetch}
        stockIn={selectedStockIn}
      />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={() => !isDeleting && setIsDeleteModalOpen(false)}
        onConfirm={confirmDelete}
        title="Delete Stock In"
        message={`Are you sure you want to delete stock in record "${stockInToDelete?.code}"?`}
        confirmText="Delete"
        isDestructive={true}
        isLoading={isDeleting}
      />
    </>
  );
}
