"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import ClearFilterButton from "@/components/common/ClearFilterButton";
import DatePicker from "@/components/form/date-picker";
import ExportButton from "@/components/common/ExportButton";
import { Modal } from "@/components/ui/modal";
import { ArrowRightIcon, ChevronLeftIcon } from "@/icons";
import { apiDownload, apiGet } from "@/lib/api";
import type { LeakTestWorkRecord } from "./types";
import { todayParam } from "./ui";

const PRESSURE_UNIT = "MPa";
const DEFAULT_TABLE_PAGE_SIZE = 10;
const TABLE_PAGE_SIZE_OPTIONS = [10, 25, 50, 0];
const datePickerInputClass = "h-10 rounded-lg border-gray-200 bg-white px-4 pr-10 text-sm font-black text-slate-900 shadow-theme-xs focus:border-brand-400 focus:ring-brand-400/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white";

function displayDate(value: string) {
  return new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(value));
}

function displayTime(value: string) {
  return value.split(".")[0].slice(0, 5);
}

function displayDateTime(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value || "-";
  }

  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    month: "short",
    year: "numeric",
  }).format(date);
}

function displayPressure(value: number) {
  return `${Number(value).toFixed(2)} ${PRESSURE_UNIT}`;
}

function dateToParam(date: Date) {
  const offset = date.getTimezoneOffset();
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 10);
}

function paramToDate(value: string) {
  const [year, month, day] = value.split("-").map(Number);
  return new Date(year, month - 1, day);
}

function getVisiblePages(currentPage: number, totalPages: number) {
  const pageCount = Math.min(5, totalPages);
  const start = Math.min(Math.max(currentPage - 2, 1), Math.max(totalPages - pageCount + 1, 1));
  return Array.from({ length: pageCount }, (_, index) => start + index);
}

function sanitizeFileName(value: string) {
  return value
    .replace(/[\\/:*?"<>|]+/g, "-")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .slice(0, 90) || "work-record";
}

function fileDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return sanitizeFileName(value);
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}${month}${day}`;
}

async function exportWorkRecordToXlsx(record: LeakTestWorkRecord) {
  const blob = await apiDownload(`/api/leaktester/work-records/${record.id}/export`);
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  const fileName = [
    sanitizeFileName(record.engine_model),
    sanitizeFileName(record.engine_number),
    fileDate(record.check_date),
    "Judgement",
    sanitizeFileName(record.result).toUpperCase(),
  ].join("_");

  link.href = url;
  link.download = `${fileName}.xlsx`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => window.URL.revokeObjectURL(url), 0);
}

async function exportWorkRecordListToXlsx(filterQuery: string, dateRangeStart: string, dateRangeEnd: string) {
  const query = filterQuery ? `?${filterQuery}` : "";
  const blob = await apiDownload(`/api/leaktester/work-records/export${query}`);
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  const period = dateRangeStart && dateRangeEnd
    ? (() => {
        const startDate = fileDate(dateRangeStart);
        const endDate = fileDate(dateRangeEnd);
        return startDate === endDate ? startDate : `${startDate}-${endDate}`;
      })()
    : "All";

  link.href = url;
  link.download = `LeakTest_WorkRecord_List_${period}.xlsx`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => window.URL.revokeObjectURL(url), 0);
}

function DetailItem({
  label,
  value,
}: {
  label: string;
  value: ReactNode;
}) {
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
      <p className="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{label}</p>
      <div className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{value}</div>
    </div>
  );
}

export default function WorkRecordPage() {
  const [records, setRecords] = useState<LeakTestWorkRecord[]>([]);
  const [dateRangeStart, setDateRangeStart] = useState(todayParam());
  const [dateRangeEnd, setDateRangeEnd] = useState(todayParam());
  const [selectedRecord, setSelectedRecord] = useState<LeakTestWorkRecord | null>(null);
  const [exportingRecordId, setExportingRecordId] = useState<number | null>(null);
  const [exportingList, setExportingList] = useState(false);
  const [tablePage, setTablePage] = useState(1);
  const [tablePageSize, setTablePageSize] = useState(DEFAULT_TABLE_PAGE_SIZE);
  const [message, setMessage] = useState<{ kind: "error"; text: string } | null>(null);

  const resetTableView = useCallback(() => {
    setSelectedRecord(null);
    setTablePage(1);
  }, []);

  const handleDateRangeChange = useCallback((selectedDates: Date[]) => {
    if (selectedDates.length < 2) {
      return;
    }

    const [firstDate, secondDate] = selectedDates;
    const startDate = firstDate <= secondDate ? firstDate : secondDate;
    const endDate = firstDate <= secondDate ? secondDate : firstDate;

    setDateRangeStart(dateToParam(startDate));
    setDateRangeEnd(dateToParam(endDate));
    resetTableView();
  }, [resetTableView]);

  const clearDateFilter = useCallback(() => {
    setDateRangeStart("");
    setDateRangeEnd("");
    resetTableView();
  }, [resetTableView]);

  const filterQuery = useMemo(() => {
    if (!dateRangeStart || !dateRangeEnd) {
      return "";
    }

    return `date_from=${dateRangeStart}&date_to=${dateRangeEnd}`;
  }, [dateRangeEnd, dateRangeStart]);

  const hasDateFilter = Boolean(dateRangeStart && dateRangeEnd);
  const rangeDefaultDate = useMemo(() => (
    hasDateFilter ? [paramToDate(dateRangeStart), paramToDate(dateRangeEnd)] : undefined
  ), [dateRangeEnd, dateRangeStart, hasDateFilter]);

  useEffect(() => {
    let ignore = false;
    const endpoint = filterQuery
      ? `/api/leaktester/work-records?${filterQuery}`
      : "/api/leaktester/work-records";
    const timer = window.setTimeout(() => {
      void apiGet<LeakTestWorkRecord[]>(endpoint)
        .then((items) => {
          if (ignore) return;
          setMessage(null);
          setRecords(items);
        })
        .catch((err) => {
          if (ignore) return;
          setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load work records." });
        });
    }, 0);

    return () => {
      ignore = true;
      window.clearTimeout(timer);
    };
  }, [filterQuery]);

  const handleExportWorkRecordList = useCallback(async () => {
    setExportingList(true);
    try {
      await exportWorkRecordListToXlsx(filterQuery, dateRangeStart, dateRangeEnd);
      setMessage(null);
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to export work record list." });
    } finally {
      setExportingList(false);
    }
  }, [dateRangeEnd, dateRangeStart, filterQuery]);

  const effectiveTablePageSize = tablePageSize === 0 ? Math.max(records.length, 1) : tablePageSize;
  const totalTablePages = Math.max(1, Math.ceil(records.length / effectiveTablePageSize));
  const currentTablePage = Math.min(Math.max(tablePage, 1), totalTablePages);
  const visibleTablePages = useMemo(() => getVisiblePages(currentTablePage, totalTablePages), [currentTablePage, totalTablePages]);
  const paginatedRecords = useMemo(() => {
    const start = (currentTablePage - 1) * effectiveTablePageSize;
    return records.slice(start, start + effectiveTablePageSize);
  }, [currentTablePage, effectiveTablePageSize, records]);
  const firstTableRecord = records.length ? (currentTablePage - 1) * effectiveTablePageSize + 1 : 0;
  const lastTableRecord = Math.min(currentTablePage * effectiveTablePageSize, records.length);

  const handleExportWorkRecord = useCallback(async (record: LeakTestWorkRecord) => {
    setExportingRecordId(record.id);
    try {
      await exportWorkRecordToXlsx(record);
      setMessage(null);
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to export work record." });
    } finally {
      setExportingRecordId(null);
    }
  }, []);

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Leaktester</p>
          <h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Leaktester Work Record</h1>
        </div>
        <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row sm:items-end">
          <div className="w-full sm:w-auto">
            <div className="flex items-end gap-1.5">
              <div className="min-w-0 flex-1 sm:w-[240px]">
                <DatePicker
                  className={datePickerInputClass}
                  dateFormat="d / m / Y"
                  defaultDate={rangeDefaultDate}
                  id="work-record-filter-date-range"
                  key={`work-record-filter-date-range-${dateRangeStart || "all"}-${dateRangeEnd || "all"}`}
                  label="Filter Date"
                  mode="range"
                  onChange={handleDateRangeChange}
                  placeholder="Select start and end date"
                  staticCalendar
                />
              </div>
              <ClearFilterButton disabled={!hasDateFilter} label="Clear date filter" onClick={clearDateFilter} />
            </div>
          </div>
          <ExportButton
            className="w-full sm:w-auto"
            disabled={exportingList}
            onClick={() => void handleExportWorkRecordList()}
          >
            {exportingList ? "Exporting..." : "Export XLSX"}
          </ExportButton>
        </div>
      </div>

      {message ? (
        <div className="rounded-md border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300">
          {message.text}
        </div>
      ) : null}

      <section className="overflow-hidden rounded-md border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-5 py-4 dark:border-slate-800 sm:flex-row sm:items-center sm:justify-between">
          <label className="flex items-center gap-2 text-sm font-medium text-slate-500 dark:text-slate-400">
            <span>Show</span>
            <select
              className="h-9 rounded-md border border-slate-200 bg-white px-2.5 text-sm font-bold text-slate-700 outline-none transition focus:border-brand-500 focus:ring-3 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-200"
              onChange={(event) => {
                setTablePageSize(Number(event.target.value));
                setTablePage(1);
              }}
              value={tablePageSize}
            >
              {TABLE_PAGE_SIZE_OPTIONS.map((size) => (
                <option key={size} value={size}>{size === 0 ? "All" : size}</option>
              ))}
            </select>
            <span>entries</span>
          </label>
          <span className="text-sm font-medium text-slate-500 dark:text-slate-400">
            Showing <span className="font-bold text-slate-800 dark:text-slate-100">{firstTableRecord}-{lastTableRecord}</span> of{" "}
            <span className="font-bold text-slate-800 dark:text-slate-100">{records.length}</span>
          </span>
        </div>

        <div className="overflow-x-auto px-3 pb-3 pt-3">
          <table className="leak-rounded-header-table w-full min-w-[1120px] border-separate border-spacing-0 text-left text-sm">
            <thead className="bg-transparent text-xs uppercase text-white">
              <tr className="bg-transparent">
                <th className="rounded-l-lg bg-brand-500 px-5 py-3">Engine Model</th>
                <th className="bg-brand-500 px-4 py-3">Engine Number</th>
                <th className="bg-brand-500 px-4 py-3">Date</th>
                <th className="bg-brand-500 px-4 py-3">Time</th>
                <th className="bg-brand-500 px-4 py-3">Machine Name</th>
                <th className="bg-brand-500 px-4 py-3">Parameter Pressure</th>
                <th className="bg-brand-500 px-4 py-3">Pressure Input</th>
                <th className="bg-brand-500 px-4 py-3">Cycle Time</th>
                <th className="rounded-r-lg bg-brand-500 px-5 py-3">Result</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {paginatedRecords.map((record) => (
                <tr
                  className="cursor-pointer transition hover:bg-brand-50/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40 dark:hover:bg-slate-800/70"
                  key={record.id}
                  onClick={() => setSelectedRecord(record)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" || event.key === " ") {
                      event.preventDefault();
                      setSelectedRecord(record);
                    }
                  }}
                  role="button"
                  tabIndex={0}
                  title="View work record detail"
                >
                  <td className="px-5 py-4 font-bold text-slate-900 dark:text-white">{record.engine_model}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.engine_number}</td>
                  <td className="px-4 py-4 font-semibold text-slate-600 dark:text-slate-300">{displayDate(record.check_date)}</td>
                  <td className="px-4 py-4 font-semibold text-slate-600 dark:text-slate-300">{displayTime(record.check_time)}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.machine_name}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{displayPressure(record.parameter_pressure)}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{displayPressure(record.pressure_input)}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.cycle_time_leak_test_minutes} menit</td>
                  <td className="px-5 py-4">
                    <span className={`rounded-full px-3 py-1 text-xs font-black ${record.result === "OK" ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300"}`}>
                      {record.result}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!records.length ? <p className="px-5 py-12 text-center text-sm text-slate-400 dark:text-slate-200">No work records for this date.</p> : null}
        </div>

        <div className="flex flex-col gap-3 border-t border-slate-100 px-5 py-4 text-sm dark:border-slate-800 sm:flex-row sm:items-center sm:justify-between">
          <span className="font-medium text-slate-500 dark:text-slate-400">
            Page <span className="font-bold text-slate-800 dark:text-slate-100">{currentTablePage}</span> of{" "}
            <span className="font-bold text-slate-800 dark:text-slate-100">{totalTablePages}</span>
          </span>
          <div className="flex flex-wrap items-center gap-2">
            <button
              aria-label="Previous page"
              className="inline-flex size-9 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-600 transition hover:border-brand-200 hover:bg-brand-50 hover:text-brand-600 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-300 dark:hover:bg-brand-500/10"
              disabled={currentTablePage === 1}
              onClick={() => setTablePage((current) => Math.max(current - 1, 1))}
              type="button"
            >
              <ChevronLeftIcon className="size-4" />
            </button>
            {visibleTablePages.map((page) => (
              <button
                className={`inline-flex size-9 items-center justify-center rounded-md text-sm font-bold transition ${
                  currentTablePage === page
                    ? "bg-brand-500 text-white shadow-theme-xs"
                    : "border border-slate-200 bg-white text-slate-600 hover:border-brand-200 hover:bg-brand-50 hover:text-brand-600 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-300 dark:hover:bg-brand-500/10"
                }`}
                key={page}
                onClick={() => setTablePage(page)}
                type="button"
              >
                {page}
              </button>
            ))}
            <button
              aria-label="Next page"
              className="inline-flex size-9 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-600 transition hover:border-brand-200 hover:bg-brand-50 hover:text-brand-600 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-300 dark:hover:bg-brand-500/10"
              disabled={currentTablePage === totalTablePages}
              onClick={() => setTablePage((current) => Math.min(current + 1, totalTablePages))}
              type="button"
            >
              <ArrowRightIcon className="size-4" />
            </button>
          </div>
        </div>
      </section>

      <Modal
        className="mx-4 max-w-[760px] overflow-hidden rounded-lg p-0"
        isOpen={Boolean(selectedRecord)}
        onClose={() => setSelectedRecord(null)}
      >
        {selectedRecord ? (
          <div>
            <div className="border-b border-slate-200 px-6 py-5 dark:border-slate-800">
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Work Record Detail</p>
              <h2 className="mt-2 text-xl font-black text-slate-900 dark:text-white">{selectedRecord.engine_number}</h2>
            </div>

            <div className="grid gap-3 p-6 sm:grid-cols-2">
              <DetailItem label="Engine Model" value={selectedRecord.engine_model} />
              <DetailItem label="Engine Number" value={selectedRecord.engine_number} />
              <DetailItem label="Date" value={displayDate(selectedRecord.check_date)} />
              <DetailItem label="Time" value={displayTime(selectedRecord.check_time)} />
              <DetailItem label="Machine Name" value={selectedRecord.machine_name} />
              <DetailItem label="Parameter Pressure" value={displayPressure(selectedRecord.parameter_pressure)} />
              <DetailItem label="Pressure Input" value={displayPressure(selectedRecord.pressure_input)} />
              <DetailItem label="Cycle Time" value={`${selectedRecord.cycle_time_leak_test_minutes} menit`} />
              <DetailItem
                label="Result"
                value={
                  <span className={`inline-flex rounded-full px-3 py-1 text-xs font-black ${selectedRecord.result === "OK" ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300"}`}>
                    {selectedRecord.result}
                  </span>
                }
              />
              <DetailItem label="Created At" value={displayDateTime(selectedRecord.created_at)} />
              <DetailItem label="Updated At" value={displayDateTime(selectedRecord.updated_at)} />
            </div>

            <div className="flex flex-col-reverse gap-3 border-t border-slate-200 px-6 py-4 dark:border-slate-800 sm:flex-row sm:items-center sm:justify-end">
              <ExportButton
                disabled={exportingRecordId === selectedRecord.id}
                onClick={() => void handleExportWorkRecord(selectedRecord)}
              >
                {exportingRecordId === selectedRecord.id ? "Exporting..." : "Export to XLSX"}
              </ExportButton>
              <button
                className="h-10 rounded-md bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600"
                onClick={() => setSelectedRecord(null)}
                type="button"
              >
                Close
              </button>
            </div>
          </div>
        ) : null}
      </Modal>
    </div>
  );
}
