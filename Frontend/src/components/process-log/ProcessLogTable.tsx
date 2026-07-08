"use client";

import { useEffect, useMemo, useRef } from "react";
import Badge from "@/components/ui/badge/Badge";
import DataTable, { DataTableColumn } from "@/components/common/DataTable";
import { useProcessLogs } from "@/hooks/useProcessLogs";
import { ProcessLog } from "@/services/ProcessLogService";
import { useToast } from "@/context/ToastContext";
import { ArrowUpIcon } from "@/icons";

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

const filterInputClassName =
  "h-10 w-[224px] max-w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90";

export default function ProcessLogTable() {
  const toast = useToast();
  const lastErrorRef = useRef<string | null>(null);

  const {
    data,
    error,
    isLoading,
    pagination,
    query,
    setLimit,
    setPage,
    setQuery,
  } = useProcessLogs({
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
      title: "Failed to load process logs",
    });
  }, [error, toast]);

  const handleExport = () => {
    toast.info({
      title: "Coming soon",
      message: "Export process logs will be available soon",
    });
  };

  const columns = useMemo<DataTableColumn<ProcessLog>[]>(
    () => [
      {
        key: "id",
        header: "ID",
        align: "right",
      },
      {
        key: "issueNo",
        header: "Issue No",
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
      {
        key: "details",
        header: "Details",
        align: "right",
        render: (_, row) => row.details.length,
      },
    ],
    []
  );

  return (
    <DataTable
      actions={
        <div className="flex flex-wrap items-center gap-3">
          <input
            type="text"
            placeholder="Issue No"
            className={filterInputClassName}
            value={query.issueNo}
            onChange={(event) => setQuery({ issueNo: event.target.value })}
          />

          <input
            type="text"
            placeholder="Part Number"
            className={filterInputClassName}
            value={query.partNumber}
            onChange={(event) => setQuery({ partNumber: event.target.value })}
          />

          <select
            className="h-10 rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm font-medium text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            value={
              query.isActive === undefined || query.isActive === null
                ? "all"
                : String(query.isActive)
            }
            onChange={(event) => {
              const value = event.target.value;
              setQuery({ isActive: value === "all" ? null : value === "true" });
            }}
          >
            <option value="all">All Status</option>
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </select>

          <button
            className="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#6D8AF3] px-4 text-sm font-semibold text-white shadow-theme-xs transition-colors hover:bg-[#5f7eea] focus:outline-none focus:ring-3 focus:ring-[#6D8AF3]/25 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={isLoading}
            onClick={handleExport}
            type="button"
          >
            <span className="inline-flex size-4 shrink-0 items-center justify-center">
              <ArrowUpIcon className="size-4" />
            </span>
            <span className="leading-5">Export</span>
          </button>
        </div>
      }
      columns={columns}
      data={data}
      emptyMessage="No process logs found"
      error={error}
      isLoading={isLoading}
      minWidth="900px"
      onLimitChange={setLimit}
      onPageChange={setPage}
      pagination={pagination}
      rowKey="id"
    />
  );
}
