"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import DataTable, { type DataTableColumn } from "@/components/common/DataTable";
import ExportButton from "@/components/common/ExportButton";
import { ThemeToggleButton } from "@/components/common/ThemeToggleButton";
import { Modal } from "@/components/ui/modal";
import LeaktesterBrand from "@/components/brand/LeaktesterBrand";
import { CloseIcon } from "@/icons";
import { apiDownload, apiGet, apiPost } from "@/lib/api";
import type { LeakTestResult, Operator, ReworkEngineRecord } from "./types";
import { ProductionDatePicker, todayParam } from "./ui";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];
const inputClass = "h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm font-bold text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-400/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white";
const publicInputClass = "h-12 w-full rounded-lg border border-slate-200 bg-white px-4 text-sm font-bold text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/15 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500";
const labelClass = "text-xs font-bold uppercase text-slate-500 dark:text-slate-400";

function displayDate(value: string) {
  return new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(value));
}

function displayTime(value: string) {
  return value.split(".")[0].slice(0, 5);
}

function displayPressure(value: number) {
  return `${Number(value).toFixed(2)} MPa`;
}

function displayOptional(value?: string | null) {
  return value && value.trim() ? value : "-";
}

function currentTimeValue() {
  const date = new Date();
  return `${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
}

function fileDate(value: string) {
  if (!value) return "All";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value.replace(/[^a-z0-9]+/gi, "-");
  return `${date.getFullYear()}${String(date.getMonth() + 1).padStart(2, "0")}${String(date.getDate()).padStart(2, "0")}`;
}

type FormManualPageProps = {
  publicAccess?: boolean;
};

export default function FormManualPage({ publicAccess = false }: FormManualPageProps) {
  const [records, setRecords] = useState<ReworkEngineRecord[]>([]);
  const [operators, setOperators] = useState<Operator[]>([]);
  const [busy, setBusy] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [exportingRecordId, setExportingRecordId] = useState<number | null>(null);
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [selectedRecord, setSelectedRecord] = useState<ReworkEngineRecord | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
  const [filterDate, setFilterDate] = useState(todayParam());
  const [barcodeScanFilter, setBarcodeScanFilter] = useState("");
  const [resultFilter, setResultFilter] = useState<"" | LeakTestResult>("");
  const [message, setMessage] = useState<{ kind: "ok" | "error"; text: string } | null>(null);

  const filterQuery = useMemo(() => {
    const params = new URLSearchParams();
    if (filterDate) params.set("date", filterDate);
    if (barcodeScanFilter.trim()) params.set("barcode_scan", barcodeScanFilter.trim());
    if (resultFilter) params.set("result", resultFilter);
    return params.toString();
  }, [barcodeScanFilter, filterDate, resultFilter]);

  const loadRecords = useCallback(async () => {
    const endpoint = filterQuery
      ? `/api/leaktester/rework-engine-records?${filterQuery}`
      : "/api/leaktester/rework-engine-records";

    try {
      setRecords(await apiGet<ReworkEngineRecord[]>(endpoint));
      setMessage((current) => current?.kind === "error" ? null : current);
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load rework history." });
    }
  }, [filterQuery]);

  useEffect(() => {
    void apiGet<Operator[]>("/api/leaktester/operators?status=active")
      .then(setOperators)
      .catch(() => setOperators([]));
  }, []);

  useEffect(() => {
    void loadRecords();
  }, [loadRecords]);

  const totalPages = Math.max(1, Math.ceil(records.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const paginatedRecords = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return records.slice(start, start + pageSize);
  }, [currentPage, pageSize, records]);
  const hasFilters = Boolean(filterDate || barcodeScanFilter.trim() || resultFilter);

  const columns: DataTableColumn<ReworkEngineRecord>[] = [
    {
      key: "engine_model",
      header: "Engine Model",
      render: (value) => <span className="font-bold text-slate-900 dark:text-white">{String(value || "-")}</span>,
    },
    { key: "engine_number", header: "Engine Number" },
    { key: "operator_name", header: "Operator", render: (value) => String(value || "-") },
    { key: "rework_date", header: "Date", render: (value) => displayDate(String(value)) },
    { key: "rework_time", header: "Time", render: (value) => displayTime(String(value)) },
    { key: "parameter_channel_no", header: "Channel", render: (_value, row) => displayOptional(row.parameter_channel_no) },
    { key: "parameter_pressure", header: "Parameter", render: (value) => displayPressure(Number(value)) },
    { key: "parameter_limit", header: "Pressure Limit (TP LL ~ TP UL)", render: (_value, row) => displayOptional(row.parameter_limit) },
    { key: "pressure_input", header: "Input", render: (value) => displayPressure(Number(value)) },
    {
      key: "result",
      header: "Result",
      render: (value) => {
        const result = String(value || "-");
        return (
          <span className={`rounded-full px-3 py-1 text-xs font-black ${result === "OK" ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300"}`}>
            {result}
          </span>
        );
      },
    },
    { key: "note", header: "Note" },
  ];

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    const form = new FormData(event.currentTarget);

    try {
      await apiPost<ReworkEngineRecord>("/api/leaktester/rework-engine-records", {
        barcode_scan: form.get("barcode_scan"),
        operator_id: Number(form.get("operator_id")) || null,
        parameter_pressure: Number(form.get("parameter_pressure")),
        pressure_input: Number(form.get("pressure_input")),
        result: form.get("result"),
        rework_date: todayParam(),
        rework_time: currentTimeValue(),
        note: form.get("note"),
      });

      event.currentTarget.reset();
      setIsFormModalOpen(false);
      setMessage({ kind: "ok", text: "Rework engine saved." });
      await loadRecords();
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to save rework engine." });
    } finally {
      setBusy(false);
    }
  }

  const handleExport = useCallback(async () => {
    setExporting(true);
    try {
      const query = filterQuery ? `?${filterQuery}` : "";
      const blob = await apiDownload(`/api/leaktester/rework-engine-records/export${query}`);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `Manual_Leaktest_Rework_Engine_${fileDate(filterDate)}.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => window.URL.revokeObjectURL(url), 0);
      setMessage(null);
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to export rework history." });
    } finally {
      setExporting(false);
    }
  }, [filterDate, filterQuery]);

  const handleExportRecord = useCallback(async (record: ReworkEngineRecord) => {
    setExportingRecordId(record.id);
    try {
      const blob = await apiDownload(`/api/leaktester/rework-engine-records/${record.id}/export`);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `Manual_Leaktest_${record.engine_model || "Engine"}_${record.engine_number}_${fileDate(record.rework_date)}.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => window.URL.revokeObjectURL(url), 0);
      setMessage(null);
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to export rework record." });
    } finally {
      setExportingRecordId(null);
    }
  }, []);

  return (
    <div className={publicAccess ? "mx-auto max-w-5xl space-y-5" : "space-y-6"}>
      {publicAccess ? (
        <div className="flex items-center justify-between rounded-xl border border-slate-200 bg-white/85 px-5 py-4 shadow-sm backdrop-blur dark:border-slate-800 dark:bg-slate-900/90">
          <LeaktesterBrand compact />
          <ThemeToggleButton />
        </div>
      ) : null}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Rework Engine</p>
          <h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">{publicAccess ? "Manual Rework Input" : "Form Manual"}</h1>
          {publicAccess ? (
            <p className="mt-2 max-w-2xl text-sm font-semibold text-slate-500 dark:text-slate-400">
              Input rework engine tanpa login untuk proses manual leaktest.
            </p>
          ) : null}
        </div>
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          {!publicAccess ? (
            <>
              <ExportButton disabled={exporting} onClick={() => void handleExport()}>
                {exporting ? "Exporting..." : "Export XLSX"}
              </ExportButton>
            </>
          ) : null}
          {!publicAccess ? (
            <button
              className="h-11 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600"
              onClick={() => setIsFormModalOpen(true)}
              type="button"
            >
              Input Rework
            </button>
          ) : null}
        </div>
      </div>

      {message ? (
        <div className={`rounded-md border px-4 py-3 text-sm font-medium ${message.kind === "ok" ? "border-emerald-200 bg-emerald-50 text-emerald-700" : "border-rose-200 bg-rose-50 text-rose-700"}`}>
          {message.text}
        </div>
      ) : null}

      {publicAccess ? (
        <form
          className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-lg shadow-slate-200/70 dark:border-slate-800 dark:bg-slate-900 dark:shadow-black/20"
          onSubmit={(event) => void submit(event)}
        >
          <div className="border-b border-slate-200 px-6 py-5 dark:border-slate-800">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-lg font-black text-slate-900 dark:text-white">Input Rework Engine</h2>
                <p className="mt-1 text-sm font-medium text-slate-500 dark:text-slate-300">
              Scan barcode engine, pilih operator, lalu isi parameter dan judgement.
                </p>
              </div>
              <span className="inline-flex h-9 items-center rounded-full bg-brand-50 px-4 text-xs font-black uppercase tracking-[0.14em] text-brand-600 dark:bg-brand-500/10 dark:text-brand-300">
                Manual Leaktest
              </span>
            </div>
          </div>

          <div className="grid gap-5 px-6 py-6 sm:grid-cols-2">
            <label className={labelClass}>
              Scan Barcode
              <input className={`${publicInputClass} mt-2`} name="barcode_scan" placeholder="TF65R DUMMY-LT-20260807-001" required autoFocus />
            </label>
            <label className={labelClass}>
              Operator
              <select className={`${publicInputClass} mt-2`} name="operator_id">
                <option value="">-</option>
                {operators.map((operator) => (
                  <option key={operator.id} value={operator.id}>{operator.operator_name}</option>
                ))}
              </select>
            </label>
            <label className={labelClass}>
              Parameter
              <input className={`${publicInputClass} mt-2`} inputMode="decimal" name="parameter_pressure" placeholder="0.30" required />
            </label>
            <label className={labelClass}>
              Pressure Input
              <input className={`${publicInputClass} mt-2`} inputMode="decimal" name="pressure_input" placeholder="0.30" required />
            </label>
            <label className={labelClass}>
              Result
              <select className={`${publicInputClass} mt-2`} defaultValue="OK" name="result">
                <option value="OK">OK</option>
                <option value="NG">NG</option>
              </select>
            </label>
            <label className={labelClass}>
              Note
              <input className={`${publicInputClass} mt-2`} name="note" placeholder="Optional note" />
            </label>
          </div>

          <div className="flex justify-end border-t border-slate-200 bg-slate-50 px-6 py-4 dark:border-slate-800 dark:bg-slate-950/40">
            <button className="h-11 rounded-lg bg-brand-500 px-6 text-sm font-black text-white transition hover:bg-brand-600 disabled:bg-brand-300" disabled={busy} type="submit">
              {busy ? "Saving..." : "Save Rework"}
            </button>
          </div>
        </form>
      ) : null}

      <Modal
        className="mx-4 max-w-[820px] overflow-hidden rounded-[22px] bg-white p-0 shadow-2xl dark:bg-slate-900"
        isOpen={!publicAccess && isFormModalOpen}
        onClose={() => {
          if (!busy) setIsFormModalOpen(false);
        }}
        showCloseButton={false}
      >
        <form onSubmit={(event) => void submit(event)}>
          <button
            aria-label="Close modal"
            className="absolute right-6 top-6 inline-flex size-11 items-center justify-center rounded-full bg-slate-100 text-slate-500 transition hover:bg-slate-200 hover:text-slate-900 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 dark:hover:text-white"
            disabled={busy}
            onClick={() => setIsFormModalOpen(false)}
            type="button"
          >
            <CloseIcon className="size-5" />
          </button>

          <div className="border-b border-slate-200 px-6 pb-4 pt-7 dark:border-slate-800">
            <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Rework Engine</p>
            <h2 className="mt-2 text-xl font-black text-slate-900 dark:text-white">Input Form Manual</h2>
          </div>

          <div className="grid gap-4 px-6 py-5 sm:grid-cols-2">
            <label className={labelClass}>
              Scan Barcode
              <input className={`${inputClass} mt-2`} name="barcode_scan" placeholder="TF65R DUMMY-LT-20260807-001" required autoFocus />
            </label>
            <label className={labelClass}>
              Operator
              <select className={`${inputClass} mt-2`} name="operator_id">
                <option value="">-</option>
                {operators.map((operator) => (
                  <option key={operator.id} value={operator.id}>{operator.operator_name}</option>
                ))}
              </select>
            </label>
            <label className={labelClass}>
              Parameter
              <input className={`${inputClass} mt-2`} inputMode="decimal" name="parameter_pressure" placeholder="0.30" required />
            </label>
            <label className={labelClass}>
              Pressure Input
              <input className={`${inputClass} mt-2`} inputMode="decimal" name="pressure_input" placeholder="0.30" required />
            </label>
            <label className={labelClass}>
              Result
              <select className={`${inputClass} mt-2`} defaultValue="OK" name="result">
                <option value="OK">OK</option>
                <option value="NG">NG</option>
              </select>
            </label>
            <label className={labelClass}>
              Note
              <input className={`${inputClass} mt-2`} name="note" placeholder="Optional note" />
            </label>
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-200 px-6 py-4 dark:border-slate-800">
            <button
              className="h-10 rounded-lg border border-slate-300 px-5 text-sm font-bold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
              disabled={busy}
              onClick={() => setIsFormModalOpen(false)}
              type="button"
            >
              Cancel
            </button>
            <button className="h-10 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600 disabled:bg-brand-300" disabled={busy} type="submit">
              {busy ? "Saving..." : "Save Rework"}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        className="mx-4 max-w-[760px] overflow-hidden rounded-lg p-0"
        isOpen={Boolean(selectedRecord)}
        onClose={() => setSelectedRecord(null)}
      >
        {selectedRecord ? (
          <div>
            <div className="border-b border-slate-200 px-6 py-5 dark:border-slate-800">
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Manual Leaktest</p>
              <h2 className="mt-2 text-xl font-black text-slate-900 dark:text-white">{selectedRecord.engine_number}</h2>
            </div>

            <div className="grid gap-3 p-6 sm:grid-cols-2">
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Engine Model</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{selectedRecord.engine_model || "-"}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Barcode Scan</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{selectedRecord.barcode_scan}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Operator</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{selectedRecord.operator_name || "-"}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Date / Time</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{displayDate(selectedRecord.rework_date)} / {displayTime(selectedRecord.rework_time)}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Channel No</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{displayOptional(selectedRecord.parameter_channel_no)}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Parameter Pressure</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{displayPressure(selectedRecord.parameter_pressure)}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Pressure Limit (TP LL ~ TP UL)</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{displayOptional(selectedRecord.parameter_limit)}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Pressure Input</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{displayPressure(selectedRecord.pressure_input)}</p>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Result</p>
                <span className={`mt-2 inline-flex rounded-full px-3 py-1 text-xs font-black ${selectedRecord.result === "OK" ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300"}`}>
                  {selectedRecord.result}
                </span>
              </div>
              <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                <p className={labelClass}>Note</p>
                <p className="mt-2 text-sm font-bold text-slate-900 dark:text-white">{selectedRecord.note || "-"}</p>
              </div>
            </div>

            <div className="flex flex-col-reverse gap-3 border-t border-slate-200 px-6 py-4 dark:border-slate-800 sm:flex-row sm:items-center sm:justify-end">
              <ExportButton
                disabled={exportingRecordId === selectedRecord.id}
                onClick={() => void handleExportRecord(selectedRecord)}
              >
                {exportingRecordId === selectedRecord.id ? "Exporting..." : "Export XLSX"}
              </ExportButton>
              <button
                className="h-10 rounded-md border border-slate-300 px-5 text-sm font-bold text-slate-700 transition hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
                onClick={() => setSelectedRecord(null)}
                type="button"
              >
                Close
              </button>
            </div>
          </div>
        ) : null}
      </Modal>

      {!publicAccess ? (
        <DataTable
          actions={
            <div className="flex flex-wrap items-end gap-3">
              <ProductionDatePicker
                defaultValue={filterDate}
                label="Filter Date"
                name="filter_date"
                onChange={(value) => {
                  setFilterDate(value);
                  setPage(1);
                }}
                value={filterDate}
              />
              <label className={labelClass}>
                Result
                <select
                  className={`${inputClass} mt-2 w-36`}
                  onChange={(event) => {
                    setResultFilter(event.target.value as "" | LeakTestResult);
                    setPage(1);
                  }}
                  value={resultFilter}
                >
                  <option value="">All</option>
                  <option value="OK">OK</option>
                  <option value="NG">NG</option>
                </select>
              </label>
            </div>
          }
          columns={columns}
          clearFiltersDisabled={!hasFilters}
          clearFiltersLabel="Clear rework filters"
          data={paginatedRecords}
          emptyMessage="No rework engine history."
          limitOptions={PAGE_SIZE_OPTIONS}
          minWidth="1320px"
          onLimitChange={(limit) => {
            setPageSize(limit);
            setPage(1);
          }}
          onClearFilters={() => {
            setFilterDate("");
            setBarcodeScanFilter("");
            setResultFilter("");
            setPage(1);
          }}
          onPageChange={setPage}
          onRowClick={setSelectedRecord}
          onSearchChange={(value) => {
            setBarcodeScanFilter(value);
            setPage(1);
          }}
          pagination={{
            limit: pageSize,
            page: currentPage,
            total: records.length,
            totalPage: totalPages,
          }}
          rowKey="id"
          searchPlaceholder="Search barcode, model, or engine number"
          searchValue={barcodeScanFilter}
          title="Rework Engine History"
        />
      ) : null}
    </div>
  );
}
