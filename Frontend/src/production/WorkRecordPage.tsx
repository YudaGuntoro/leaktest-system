"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import ClearFilterButton from "@/components/common/ClearFilterButton";
import DatePicker from "@/components/form/date-picker";
import ExportButton from "@/components/common/ExportButton";
import { Modal } from "@/components/ui/modal";
import { ArrowRightIcon, ChevronLeftIcon } from "@/icons";
import { apiDownload, apiGet } from "@/lib/api";
import type { EngineModel, LeakTestParameter, LeakTestResult, LeakTestWorkRecord } from "./types";
import { displayNumber, displayUnitlessText, fetchSystemSettings, getUnitSettings, type UnitSettings } from "./settings";
import { todayParam } from "./ui";

const DEFAULT_TABLE_PAGE_SIZE = 10;
const TABLE_PAGE_SIZE_OPTIONS = [10, 25, 50, 0];
const datePickerInputClass = "h-10 rounded-lg border-gray-200 bg-white px-4 pr-10 text-sm font-black text-slate-900 shadow-theme-xs focus:border-brand-400 focus:ring-brand-400/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white";
const filterInputClass = "mt-2 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm font-bold text-slate-900 shadow-theme-xs outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-400/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500";
const filterSelectClass = `${filterInputClass} pr-9`;

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

function displayOptional(value?: string | null) {
  return value && value.trim() ? value : "-";
}

function displayJudgement(record: LeakTestWorkRecord) {
  const code = record.judgement_code ?? null;
  const name = displayOptional(record.judgement_name);

  if (name !== "-") {
    return name;
  }

  return code === null ? "-" : String(code);
}

function pressureInputStateClass(result: LeakTestResult) {
  return result === "OK"
    ? "border-emerald-400/70 bg-emerald-50 text-emerald-700 dark:border-emerald-400/70 dark:bg-emerald-500/10 dark:text-emerald-300"
    : "border-rose-400/80 bg-rose-50 text-rose-700 dark:border-rose-400/80 dark:bg-rose-500/10 dark:text-rose-300";
}

function pressureInputTextClass(result: LeakTestResult) {
  return result === "OK"
    ? "text-emerald-700 dark:text-emerald-300"
    : "text-rose-700 dark:text-rose-300";
}

function normalizeModelKey(value?: string | null) {
  return String(value || "")
    .replace(/[^a-z0-9]/gi, "")
    .toUpperCase();
}

function splitCsv(value?: string | null) {
  return String(value || "")
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function findParameterValue(parameters: LeakTestParameter[], itemName: string) {
  const term = itemName.toLowerCase();
  return parameters.find((parameter) => parameter.item_name.toLowerCase().includes(term))?.item_value ?? null;
}

function formatMasterPressureLimit(lower?: string | null, upper?: string | null) {
  if (lower && upper) {
    return `${lower} ~ ${upper}`;
  }

  if (lower) return lower;
  if (upper) return upper;
  return "-";
}

function getMasterParameterContext(parameters: LeakTestParameter[], engineModel: string) {
  const modelKey = normalizeModelKey(engineModel);
  if (!modelKey || !parameters.length) {
    return { channelNo: "-", limit: "-" };
  }

  const groups = new Map<string, LeakTestParameter[]>();
  parameters.forEach((parameter) => {
    const channelNo = parameter.channel_no.trim();
    if (!channelNo) return;
    groups.set(channelNo, [...(groups.get(channelNo) ?? []), parameter]);
  });

  const groupedParameters = Array.from(groups.values());
  const exactGroup = groupedParameters.find((group) =>
    group.some((parameter) =>
      splitCsv(parameter.machine_names).some((machine) => normalizeModelKey(machine) === modelKey)
    )
  );

  const prefixGroup = exactGroup ?? groupedParameters
    .map((group) => ({
      group,
      score: Math.max(
        0,
        ...group.flatMap((parameter) =>
          splitCsv(parameter.model_parameter)
            .map(normalizeModelKey)
            .filter((parameterKey) => parameterKey && modelKey.startsWith(parameterKey))
            .map((parameterKey) => parameterKey.length)
        )
      ),
    }))
    .filter((item) => item.score > 0)
    .sort((left, right) => right.score - left.score)[0]?.group;

  if (!prefixGroup?.length) {
    return { channelNo: "-", limit: "-" };
  }

  const lower = findParameterValue(prefixGroup, "lower press limit");
  const upper = findParameterValue(prefixGroup, "upper press limit");
  return {
    channelNo: prefixGroup[0].channel_no,
    limit: formatMasterPressureLimit(lower, upper),
  };
}

function displayBarcode(record: LeakTestWorkRecord) {
  return displayOptional(record.barcode_scan) === "-"
    ? `${record.engine_model} ${record.engine_number}`.trim()
    : displayOptional(record.barcode_scan);
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
  className = "",
  label,
  value,
  valueClassName = "text-slate-900 dark:text-white",
}: {
  className?: string;
  label: string;
  value: ReactNode;
  valueClassName?: string;
}) {
  const surfaceClass = className || "border-slate-200 bg-slate-50 dark:border-slate-800 dark:bg-slate-950";

  return (
    <div className={`rounded-lg border px-4 py-3 ${surfaceClass}`}>
      <p className="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{label}</p>
      <div className={`mt-2 text-sm font-bold ${valueClassName}`}>{value}</div>
    </div>
  );
}

export default function WorkRecordPage() {
  const [records, setRecords] = useState<LeakTestWorkRecord[]>([]);
  const [engineModels, setEngineModels] = useState<EngineModel[]>([]);
  const [parameters, setParameters] = useState<LeakTestParameter[]>([]);
  const [dateRangeStart, setDateRangeStart] = useState(todayParam());
  const [dateRangeEnd, setDateRangeEnd] = useState(todayParam());
  const [engineModelFilter, setEngineModelFilter] = useState("");
  const [barcodeScanFilter, setBarcodeScanFilter] = useState("");
  const [resultFilter, setResultFilter] = useState<"" | LeakTestResult>("");
  const [selectedRecord, setSelectedRecord] = useState<LeakTestWorkRecord | null>(null);
  const [exportingRecordId, setExportingRecordId] = useState<number | null>(null);
  const [exportingList, setExportingList] = useState(false);
  const [tablePage, setTablePage] = useState(1);
  const [tablePageSize, setTablePageSize] = useState(DEFAULT_TABLE_PAGE_SIZE);
  const [message, setMessage] = useState<{ kind: "error"; text: string } | null>(null);
  const [unitSettings, setUnitSettings] = useState<UnitSettings>(() => getUnitSettings());
  const pressureUnit = unitSettings.pressureUnit.trim() || "MPa";
  const cycleTimeUnit = unitSettings.cycleTimeUnit.trim() || "s";

  useEffect(() => {
    let ignore = false;
    void fetchSystemSettings().then((settings) => {
      if (!ignore) {
        setUnitSettings({
          cycleTimeUnit: settings.cycleTimeUnit,
          pressureUnit: settings.pressureUnit,
        });
      }
    });

    return () => {
      ignore = true;
    };
  }, []);

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

  const clearRecordFilters = useCallback(() => {
    setEngineModelFilter("");
    setBarcodeScanFilter("");
    setResultFilter("");
    resetTableView();
  }, [resetTableView]);

  const filterQuery = useMemo(() => {
    const params = new URLSearchParams();

    if (dateRangeStart && dateRangeEnd) {
      params.set("date_from", dateRangeStart);
      params.set("date_to", dateRangeEnd);
    }

    if (engineModelFilter) {
      params.set("engine_model", engineModelFilter);
    }

    if (resultFilter) {
      params.set("result", resultFilter);
    }

    const barcodeScanTerm = barcodeScanFilter.trim();
    if (barcodeScanTerm) {
      params.set("barcode_scan", barcodeScanTerm);
    }

    return params.toString();
  }, [barcodeScanFilter, dateRangeEnd, dateRangeStart, engineModelFilter, resultFilter]);

  const hasDateFilter = Boolean(dateRangeStart && dateRangeEnd);
  const hasRecordFilters = Boolean(engineModelFilter || barcodeScanFilter.trim() || resultFilter);
  const rangeDefaultDate = useMemo(() => (
    hasDateFilter ? [paramToDate(dateRangeStart), paramToDate(dateRangeEnd)] : undefined
  ), [dateRangeEnd, dateRangeStart, hasDateFilter]);

  useEffect(() => {
    let ignore = false;

    void apiGet<EngineModel[]>("/api/leaktester/engine-models?status=active")
      .then((items) => {
        if (ignore) return;
        setEngineModels(items);
      })
      .catch(() => {
        if (ignore) return;
        setEngineModels([]);
      });

    void apiGet<LeakTestParameter[]>("/api/leaktester/parameters?status=active")
      .then((items) => {
        if (ignore) return;
        setParameters(items);
      })
      .catch(() => {
        if (ignore) return;
        setParameters([]);
      });

    return () => {
      ignore = true;
    };
  }, []);

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
        <div className="border-b border-slate-100 px-5 py-4 dark:border-slate-800">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
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

          <div className="mt-4 grid gap-3 sm:grid-cols-[minmax(0,240px)_minmax(0,300px)_minmax(0,140px)_auto] sm:items-end">
            <label className="block text-xs font-bold uppercase text-slate-500 dark:text-slate-400">
              Engine Model
              <select
                className={filterSelectClass}
                onChange={(event) => {
                  setEngineModelFilter(event.target.value);
                  resetTableView();
                }}
                value={engineModelFilter}
              >
                <option value="">All Engine Models</option>
                {engineModels.map((item) => (
                  <option key={item.id} value={item.engine_model}>{item.engine_model}</option>
                ))}
              </select>
            </label>
            <label className="block text-xs font-bold uppercase text-slate-500 dark:text-slate-400">
              Barcode Scan
              <input
                className={filterInputClass}
                onChange={(event) => {
                  setBarcodeScanFilter(event.target.value);
                  resetTableView();
                }}
                placeholder="Scan barcode"
                value={barcodeScanFilter}
              />
            </label>
            <label className="block text-xs font-bold uppercase text-slate-500 dark:text-slate-400">
              Result
              <select
                className={filterSelectClass}
                onChange={(event) => {
                  setResultFilter(event.target.value as "" | LeakTestResult);
                  resetTableView();
                }}
                value={resultFilter}
              >
                <option value="">All Results</option>
                <option value="OK">OK</option>
                <option value="NG">NG</option>
              </select>
            </label>
            <ClearFilterButton disabled={!hasRecordFilters} label="Clear work record filters" onClick={clearRecordFilters} />
          </div>
        </div>

        <div className="overflow-x-auto px-3 pb-3 pt-3">
          <table className="leak-rounded-header-table w-full min-w-[1480px] border-separate border-spacing-0 text-left text-sm">
            <thead className="bg-transparent text-xs uppercase text-white">
              <tr className="bg-transparent">
                <th className="rounded-l-lg bg-brand-500 px-5 py-3">Engine Model</th>
                <th className="bg-brand-500 px-4 py-3">Serial No</th>
                <th className="bg-brand-500 px-4 py-3">Barcode Scan</th>
                <th className="bg-brand-500 px-4 py-3">Operator Code</th>
                <th className="bg-brand-500 px-4 py-3">Operator Name</th>
                <th className="bg-brand-500 px-4 py-3">Date</th>
                <th className="bg-brand-500 px-4 py-3">Time</th>
                <th className="bg-brand-500 px-4 py-3">Channel</th>
                <th className="bg-brand-500 px-4 py-3">Pressure Limit (TP LL ~ TP UL) ({pressureUnit})</th>
                <th className="bg-brand-500 px-4 py-3">Pressure Input ({pressureUnit})</th>
                <th className="bg-brand-500 px-4 py-3">Cycle Time ({cycleTimeUnit})</th>
                <th className="bg-brand-500 px-4 py-3">Judgement Name</th>
                <th className="rounded-r-lg bg-brand-500 px-5 py-3">Result</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {paginatedRecords.map((record) => {
                const parameterContext = getMasterParameterContext(parameters, record.engine_model);
                return (
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
                    <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{displayBarcode(record)}</td>
                    <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.operator_code || "-"}</td>
                    <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.operator_name || "-"}</td>
                    <td className="px-4 py-4 font-semibold text-slate-600 dark:text-slate-300">{displayDate(record.check_date)}</td>
                    <td className="px-4 py-4 font-semibold text-slate-600 dark:text-slate-300">{displayTime(record.check_time)}</td>
                    <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{parameterContext.channelNo}</td>
                    <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{displayUnitlessText(parameterContext.limit)}</td>
                    <td className="px-4 py-4">
                      <span className={`inline-flex rounded-md border px-2.5 py-1 text-xs font-black ${pressureInputStateClass(record.result)}`}>
                        {displayNumber(record.pressure_input)}
                      </span>
                    </td>
                    <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.cycle_time_leak_test_minutes}</td>
                    <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{displayJudgement(record)}</td>
                    <td className="px-5 py-4">
                      <span className={`rounded-full px-3 py-1 text-xs font-black ${record.result === "OK" ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300"}`}>
                        {record.result}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {!records.length ? <p className="px-5 py-12 text-center text-sm text-slate-400 dark:text-slate-200">No work records match the selected filters.</p> : null}
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
        {selectedRecord ? (() => {
          const parameterContext = getMasterParameterContext(parameters, selectedRecord.engine_model);
          return (
          <div>
            <div className="border-b border-slate-200 px-6 py-5 dark:border-slate-800">
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Work Record Detail</p>
              <h2 className="mt-2 text-xl font-black text-slate-900 dark:text-white">{selectedRecord.engine_number}</h2>
            </div>

            <div className="grid gap-3 p-6 sm:grid-cols-2">
              <DetailItem label="Engine Model" value={selectedRecord.engine_model} />
              <DetailItem label="Serial No" value={selectedRecord.engine_number} />
              <DetailItem label="Barcode Scan" value={displayBarcode(selectedRecord)} />
              <DetailItem label="Date" value={displayDate(selectedRecord.check_date)} />
              <DetailItem label="Time" value={displayTime(selectedRecord.check_time)} />
              <DetailItem label="Operator Code" value={selectedRecord.operator_code || "-"} />
              <DetailItem label="Operator Name" value={selectedRecord.operator_name || "-"} />
              <DetailItem label="Channel No" value={parameterContext.channelNo} />
              <DetailItem label={`Pressure Limit (TP LL ~ TP UL) (${pressureUnit})`} value={displayUnitlessText(parameterContext.limit)} />
              <DetailItem label={`Pressure Setting (${pressureUnit})`} value={displayNumber(selectedRecord.parameter_pressure)} />
              <DetailItem
                className={pressureInputStateClass(selectedRecord.result)}
                label={`Pressure Input (${pressureUnit})`}
                value={displayNumber(selectedRecord.pressure_input)}
                valueClassName={pressureInputTextClass(selectedRecord.result)}
              />
              <DetailItem label={`Cycle Time (${cycleTimeUnit})`} value={selectedRecord.cycle_time_leak_test_minutes} />
              <DetailItem label="Judgement Name" value={displayJudgement(selectedRecord)} />
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
          );
        })() : null}
      </Modal>
    </div>
  );
}
