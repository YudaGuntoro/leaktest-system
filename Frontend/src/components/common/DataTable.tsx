"use client";

import React, { useState, useEffect } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export type DataTableSortDirection = "asc" | "desc";

export type DataTableColumn<T extends object> = {
  key: string;
  header: React.ReactNode;
  accessor?: keyof T | string | ((row: T) => React.ReactNode);
  render?: (value: unknown, row: T, index: number) => React.ReactNode;
  align?: "left" | "center" | "right";
  className?: string;
  headerClassName?: string;
  sortable?: boolean;
  width?: string;
};

export type DataTablePagination = {
  page: number;
  limit: number;
  total: number;
  totalPage: number;
};

type SearchFieldOption = {
  label: string;
  value: string;
};

type DataTableProps<T extends object> = {
  actions?: React.ReactNode;
  columns: DataTableColumn<T>[];
  data: T[];
  emptyMessage?: string;
  error?: string | null;
  isLoading?: boolean;
  limitOptions?: number[];
  minWidth?: string;
  onLimitChange?: (limit: number) => void;
  onPageChange?: (page: number) => void;
  onSearchChange?: (value: string) => void;
  onSearchFieldChange?: (value: string) => void;
  onSortChange?: (key: string, direction: DataTableSortDirection) => void;
  pagination?: DataTablePagination;
  rowKey?: keyof T | ((row: T, index: number) => string | number);
  searchField?: string;
  searchFields?: SearchFieldOption[];
  searchPlaceholder?: string;
  searchValue?: string;
  sortDirection?: DataTableSortDirection;
  sortKey?: string;
  title?: string;
};

const alignClasses = {
  center: "text-center",
  left: "text-left",
  right: "text-right",
};

const getNestedValue = <T extends object>(row: T, key: string) => {
  return key.split(".").reduce<unknown>((currentValue, path) => {
    if (currentValue && typeof currentValue === "object") {
      return (currentValue as Record<string, unknown>)[path];
    }

    return undefined;
  }, row);
};

const getColumnValue = <T extends object>(
  row: T,
  column: DataTableColumn<T>
) => {
  if (typeof column.accessor === "function") {
    return column.accessor(row);
  }

  if (typeof column.accessor === "string") {
    return getNestedValue(row, column.accessor);
  }

  return getNestedValue(row, column.key);
};

const formatCellValue = (value: unknown) => {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }

  if (React.isValidElement(value)) {
    return value;
  }

  return String(value);
};

const getRowKey = <T extends object>(
  row: T,
  index: number,
  rowKey?: DataTableProps<T>["rowKey"]
) => {
  if (typeof rowKey === "function") {
    return rowKey(row, index);
  }

  if (rowKey) {
    const value = row[rowKey];
    return typeof value === "string" || typeof value === "number"
      ? value
      : index;
  }

  const id = (row as Record<string, unknown>).id;
  return typeof id === "string" || typeof id === "number" ? id : index;
};

const getPageNumbers = (currentPage: number, totalPage: number) => {
  const pageNumbers: number[] = [];
  const start = Math.max(1, currentPage - 1);
  const end = Math.min(totalPage, currentPage + 1);

  for (let page = start; page <= end; page += 1) {
    pageNumbers.push(page);
  }

  return pageNumbers;
};

export default function DataTable<T extends object>({
  actions,
  columns,
  data,
  emptyMessage = "No data available",
  error,
  isLoading = false,
  limitOptions = [10, 25, 50, 100],
  minWidth = "720px",
  onLimitChange,
  onPageChange,
  onSearchChange,
  onSearchFieldChange,
  onSortChange,
  pagination,
  rowKey,
  searchField,
  searchFields,
  searchPlaceholder = "Search",
  searchValue,
  sortDirection,
  sortKey,
  title,
}: DataTableProps<T>) {
  const normalizedSearchValue = searchValue ?? "";
  const [localSearchValue, setLocalSearchValue] = useState(normalizedSearchValue);
  const [lastSearchValue, setLastSearchValue] = useState(normalizedSearchValue);

  if (lastSearchValue !== normalizedSearchValue) {
    setLastSearchValue(normalizedSearchValue);
    setLocalSearchValue(normalizedSearchValue);
  }

  // Debounce search change
  useEffect(() => {
    if (localSearchValue === normalizedSearchValue) return;

    const timer = setTimeout(() => {
      onSearchChange?.(localSearchValue);
    }, 500);

    return () => clearTimeout(timer);
  }, [localSearchValue, onSearchChange, normalizedSearchValue]);

  const currentPage = pagination?.page ?? 1;
  const currentLimit = pagination?.limit ?? limitOptions[0];
  const totalPage = pagination?.totalPage ?? 1;
  const total = pagination?.total ?? data.length;
  const firstItem = data.length > 0 ? (currentPage - 1) * currentLimit + 1 : 0;
  const lastItem = data.length > 0 ? firstItem + data.length - 1 : 0;
  const pageNumbers = getPageNumbers(currentPage, totalPage);

  const handleSort = (column: DataTableColumn<T>) => {
    if (!column.sortable || !onSortChange) {
      return;
    }

    const nextDirection =
      sortKey === column.key && sortDirection === "asc" ? "desc" : "asc";
    onSortChange(column.key, nextDirection);
  };

  return (
    <div className="overflow-hidden rounded-xl border border-gray-200 bg-white dark:border-white/[0.05] dark:bg-white/[0.03] mx-4 my-4">
      {(title || actions || onSearchChange || onLimitChange) && (
        <div className="border-b border-gray-100 px-5 py-4 dark:border-white/[0.05]">
          {title && (
            <h3 className="mb-4 text-base font-semibold text-gray-800 dark:text-white/90">
              {title}
            </h3>
          )}

          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap items-center gap-3">
              {onLimitChange && (
                <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                  Show
                  <select
                    className="h-10 rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
                    value={currentLimit}
                    onChange={(event) =>
                      onLimitChange(Number(event.target.value))
                    }
                  >
                    {limitOptions.map((limit) => (
                      <option key={limit} value={limit}>
                        {limit}
                      </option>
                    ))}
                  </select>
                  entries
                </label>
              )}

              {actions}
            </div>

            {(onSearchChange || searchFields) && (
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
                {onSearchChange && (
                  <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                    Search
                    <input
                      className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90 sm:w-64"
                      placeholder={searchPlaceholder}
                      value={localSearchValue}
                      onChange={(event) => setLocalSearchValue(event.target.value)}
                    />
                  </label>
                )}

                {searchFields && onSearchFieldChange && (
                  <select
                    className="h-10 rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm font-medium text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
                    value={searchField}
                    onChange={(event) =>
                      onSearchFieldChange(event.target.value)
                    }
                  >
                    {searchFields.map((field) => (
                      <option key={field.value} value={field.value}>
                        {field.label}
                      </option>
                    ))}
                  </select>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      <div className="mx-4 mb-4 mt-2 rounded-lg overflow-hidden border border-gray-100 dark:border-white/[0.05]">
        <div className="max-w-full overflow-x-auto">
        <div style={{ minWidth }}>
          <Table>
            <TableHeader className="border-b border-brand-100 dark:border-brand-500/20">
              <TableRow>
                {columns.map((column) => (
                  <TableCell
                    key={column.key}
                    isHeader
                    className={`px-5 py-3 text-theme-xs font-semibold text-white ${
                      alignClasses[column.align ?? "left"]
                    } ${column.headerClassName ?? ""}`}
                  >
                    {column.sortable && onSortChange ? (
                      <button
                        className="inline-flex items-center gap-1 font-medium"
                        onClick={() => handleSort(column)}
                        style={{ width: column.width }}
                        type="button"
                      >
                        {column.header}
                        <span className="text-brand-400 dark:text-brand-300/80">
                          {sortKey === column.key
                            ? sortDirection === "desc"
                              ? "DESC"
                              : "ASC"
                            : "SORT"}
                        </span>
                      </button>
                    ) : (
                      <span style={{ width: column.width }}>
                        {column.header}
                      </span>
                    )}
                  </TableCell>
                ))}
              </TableRow>
            </TableHeader>

            <TableBody className="divide-y divide-gray-100 dark:divide-white/[0.05]">
              {isLoading &&
                Array.from({ length: currentLimit }).map((_, index) => (
                  <TableRow key={`loading-${index}`}>
                    {columns.map((column) => (
                      <TableCell key={column.key} className="px-5 py-4">
                        <div className="h-4 w-full animate-pulse rounded bg-gray-100 dark:bg-white/[0.05]" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}

              {!isLoading && error && (
                <TableRow>
                  <TableCell
                    className="px-5 py-8 text-center text-sm text-error-600 dark:text-error-400"
                    colSpan={columns.length}
                  >
                    {error}
                  </TableCell>
                </TableRow>
              )}

              {!isLoading && !error && data.length === 0 && (
                <TableRow>
                  <TableCell
                    className="px-5 py-8 text-center text-sm text-gray-500 dark:text-gray-400"
                    colSpan={columns.length}
                  >
                    {emptyMessage}
                  </TableCell>
                </TableRow>
              )}

              {!isLoading &&
                !error &&
                data.map((row, index) => (
                  <TableRow key={getRowKey(row, index, rowKey)}>
                    {columns.map((column) => {
                      const value = getColumnValue(row, column);

                      return (
                        <TableCell
                          key={column.key}
                          className={`px-5 py-4 text-theme-sm text-gray-700 dark:text-gray-300 ${
                            alignClasses[column.align ?? "left"]
                          } ${column.className ?? ""}`}
                        >
                          {column.render
                            ? column.render(value, row, index)
                            : formatCellValue(value)}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                ))}
            </TableBody>
          </Table>
          </div>
        </div>
      </div>

      {pagination && (
        <div className="flex flex-col gap-3 border-t border-gray-100 px-5 py-4 text-sm text-gray-500 dark:border-white/[0.05] dark:text-gray-400 sm:flex-row sm:items-center sm:justify-between">
          <span>
            Showing {firstItem} to {lastItem} of {total} entries
          </span>

          {onPageChange && (
            <div className="flex items-center gap-2">
              <button
                className="rounded-lg border border-gray-300 px-3 py-2 font-medium text-gray-700 disabled:cursor-not-allowed disabled:opacity-50 dark:border-gray-700 dark:text-gray-300"
                disabled={currentPage <= 1 || isLoading}
                onClick={() => onPageChange(currentPage - 1)}
                type="button"
              >
                Prev
              </button>

              {pageNumbers.map((page) => (
                <button
                  key={page}
                  className={`h-10 min-w-10 rounded-lg border px-3 text-sm font-medium ${
                    page === currentPage
                      ? "border-brand-500 bg-brand-500 text-white"
                      : "border-gray-300 text-gray-700 dark:border-gray-700 dark:text-gray-300"
                  }`}
                  disabled={isLoading}
                  onClick={() => onPageChange(page)}
                  type="button"
                >
                  {page}
                </button>
              ))}

              <button
                className="rounded-lg border border-gray-300 px-3 py-2 font-medium text-gray-700 disabled:cursor-not-allowed disabled:opacity-50 dark:border-gray-700 dark:text-gray-300"
                disabled={currentPage >= totalPage || isLoading}
                onClick={() => onPageChange(currentPage + 1)}
                type="button"
              >
                Next
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
