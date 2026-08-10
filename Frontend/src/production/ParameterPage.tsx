"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import CreateButton from "@/components/common/CreateButton";
import DataTable, { type DataTableColumn } from "@/components/common/DataTable";
import { Modal } from "@/components/ui/modal";
import { CloseIcon } from "@/icons";
import { apiGet, apiPost, apiRequest } from "@/lib/api";
import type { EngineModel, LeakTestParameter, LeakTestParameterImportResult } from "./types";

type ParameterStatusFilter = "active" | "all" | "deleted";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];
const modalInputClass = "mt-2 h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm font-semibold text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-600 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500";
const selectClass = "h-10 rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm font-medium text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90";

function splitMachines(value?: string | null) {
  return String(value || "")
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function machineBadges(row: LeakTestParameter, onOpen: (row: LeakTestParameter) => void) {
  const machines = splitMachines(row.machine_names);
  const visible = machines.slice(0, 5);
  const hidden = machines.length - visible.length;

  if (!machines.length) return "-";

  return (
    <div className="flex max-w-[560px] flex-wrap gap-1.5">
      {visible.map((machine) => (
        <button
          className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600 transition hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
          key={machine}
          onClick={() => onOpen(row)}
          type="button"
        >
          {machine}
        </button>
      ))}
      {hidden > 0 ? (
        <button
          className="rounded-full bg-brand-50 px-2.5 py-1 text-xs font-bold text-brand-600 transition hover:bg-brand-100 dark:bg-brand-500/10 dark:text-brand-300 dark:hover:bg-brand-500/20"
          onClick={() => onOpen(row)}
          type="button"
        >
          +{hidden}
        </button>
      ) : null}
    </div>
  );
}

type MachineMultiSelectProps = {
  options: string[];
  selected: string[];
  onChange: (selected: string[]) => void;
};

function MachineMultiSelect({ options, selected, onChange }: MachineMultiSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState("");

  const filteredOptions = useMemo(() => {
    const term = search.trim().toLowerCase();
    return term
      ? options.filter((machine) => machine.toLowerCase().includes(term))
      : options;
  }, [options, search]);

  const toggle = (machine: string) => {
    onChange(
      selected.includes(machine)
        ? selected.filter((item) => item !== machine)
        : [...selected, machine]
    );
  };

  const toggleOpen = () => {
    setIsOpen((current) => {
      const next = !current;
      if (next) setSearch("");
      return next;
    });
  };

  return (
    <div className="relative mt-2">
      <button
        className="flex min-h-12 w-full items-center justify-between gap-3 rounded-lg border border-slate-300 bg-white px-3 py-2 text-left text-sm font-medium text-slate-900 outline-none transition hover:border-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-600 dark:bg-slate-950 dark:text-white dark:hover:border-slate-500"
        onClick={toggleOpen}
        type="button"
      >
        <span className="flex flex-1 flex-wrap gap-2">
          {selected.length ? (
            selected.map((machine) => (
              <span
                className="inline-flex items-center gap-1 rounded-full bg-slate-900 px-2.5 py-1 text-xs font-bold text-white dark:bg-slate-800"
                key={machine}
              >
                {machine}
                <span
                  className="text-slate-400 hover:text-white"
                  onClick={(event) => {
                    event.stopPropagation();
                    toggle(machine);
                  }}
                >
                  x
                </span>
              </span>
            ))
          ) : (
            <span className="py-1 text-slate-400">Select machine models</span>
          )}
        </span>
        <span className={`text-slate-500 transition dark:text-slate-400 ${isOpen ? "rotate-180" : ""}`}>v</span>
      </button>

      {isOpen ? (
        <div className="absolute left-0 right-0 top-full z-[9999] mt-2 max-h-[320px] overflow-y-auto rounded-lg border border-slate-200 bg-white shadow-2xl dark:border-slate-700 dark:bg-slate-950">
          <div className="sticky top-0 z-10 border-b border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-950">
            <input
              autoFocus
              className="h-10 w-full rounded-lg border border-slate-300 bg-slate-50 px-3 text-sm font-bold text-slate-900 outline-none placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-700 dark:bg-slate-900 dark:text-white dark:placeholder:text-slate-500"
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Type model, e.g. TF115"
              value={search}
            />
          </div>
          {filteredOptions.map((machine) => {
            const checked = selected.includes(machine);
            return (
              <button
                className={`flex w-full items-center justify-between border-b border-slate-100 px-4 py-3 text-left text-sm font-bold text-slate-800 transition last:border-b-0 hover:bg-slate-50 dark:border-slate-800 dark:text-slate-100 dark:hover:bg-slate-900 ${checked ? "bg-brand-50 text-brand-600 dark:bg-brand-500/10 dark:text-brand-300" : ""}`}
                key={machine}
                onClick={() => toggle(machine)}
                type="button"
              >
                {machine}
                {checked ? <span className="text-brand-600 dark:text-brand-300">selected</span> : null}
              </button>
            );
          })}
          {!filteredOptions.length ? (
            <div className="px-4 py-5 text-center text-sm font-bold text-slate-400">
              No model found
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

export default function ParameterPage() {
  const [items, setItems] = useState<LeakTestParameter[]>([]);
  const [engineModels, setEngineModels] = useState<EngineModel[]>([]);
  const [busy, setBusy] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [editingParameter, setEditingParameter] = useState<LeakTestParameter | null>(null);
  const [selectedMachineParameter, setSelectedMachineParameter] = useState<LeakTestParameter | null>(null);
  const [selectedMachines, setSelectedMachines] = useState<string[]>([]);
  const [message, setMessage] = useState<{ kind: "ok" | "error"; text: string } | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState<ParameterStatusFilter>("active");
  const hasFilters = Boolean(searchText.trim()) || statusFilter !== "active";

  const columns: DataTableColumn<LeakTestParameter>[] = [
    {
      key: "channel_no",
      header: "Channel No",
      rowSpanKey: (row) => row.channel_no,
      render: (value) => <span className="font-bold text-slate-900 dark:text-white">{String(value || "-")}</span>,
    },
    {
      key: "model_parameter",
      header: "Model Parameter",
      rowSpanKey: (row) => `${row.channel_no}::${row.model_parameter}`,
    },
    { key: "item_name", header: "Item Name" },
    {
      key: "item_value",
      header: "Value",
      render: (value) => <span className="font-bold text-brand-600 dark:text-brand-300">{String(value || "-")}</span>,
    },
    {
      key: "machine_names",
      header: "Machine Name",
      render: (_value, row) => machineBadges(row, setSelectedMachineParameter),
    },
    {
      key: "is_deleted",
      header: "Status",
      render: (value) => {
        const isDeleted = Boolean(value);

        return (
          <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${isDeleted ? "bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-300" : "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300"}`}>
            {isDeleted ? "DELETED" : "ACTIVE"}
          </span>
        );
      },
    },
    {
      align: "right",
      key: "action",
      header: "Action",
      render: (_value, row) => (
        <button
          className="rounded-lg border border-brand-200 px-3 py-1.5 text-xs font-bold text-brand-600 transition hover:bg-brand-50 dark:border-brand-500/30 dark:text-brand-300 dark:hover:bg-brand-500/10"
          onClick={() => {
            setEditingParameter(row);
            setSelectedMachines(splitMachines(row.machine_names));
            setIsCreateModalOpen(true);
          }}
          type="button"
        >
          Edit
        </button>
      ),
    },
  ];

  const filterQuery = useMemo(() => {
    const params = new URLSearchParams();
    const term = searchText.trim();

    if (term) params.set("search", term);
    params.set("status", statusFilter);

    return `?${params.toString()}`;
  }, [searchText, statusFilter]);

  const load = useCallback(async () => {
    try {
      setItems(await apiGet<LeakTestParameter[]>(`/api/leaktester/parameters${filterQuery}`));
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load parameter data." });
    }
  }, [filterQuery]);

  const clearFilters = useCallback(() => {
    setSearchText("");
    setStatusFilter("active");
    setPage(1);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    void apiGet<EngineModel[]>("/api/leaktester/engine-models?status=active")
      .then(setEngineModels)
      .catch(() => setEngineModels([]));
  }, []);

  const machineOptions = useMemo(() => {
    const values = new Set<string>();

    engineModels.forEach((model) => {
      if (model.engine_model) values.add(model.engine_model);
    });

    items.forEach((item) => {
      splitMachines(item.machine_names).forEach((machine) => values.add(machine));
    });

    selectedMachines.forEach((machine) => values.add(machine));

    return [...values].sort((first, second) => first.localeCompare(second));
  }, [engineModels, items, selectedMachines]);

  const totalPages = Math.max(1, Math.ceil(items.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const paginatedItems = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [currentPage, items, pageSize]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    const form = new FormData(event.currentTarget);

    try {
      const payload = {
        channel_no: form.get("channel_no"),
        model_parameter: form.get("model_parameter"),
        item_name: form.get("item_name"),
        item_value: form.get("item_value"),
        machine_names: selectedMachines.join(", "),
        is_deleted: form.get("is_active") !== "on",
      };

      if (editingParameter) {
        await apiRequest<LeakTestParameter>(`/api/leaktester/parameters/${editingParameter.id}`, {
          body: JSON.stringify(payload),
          method: "PUT",
        });
      } else {
        await apiPost<LeakTestParameter>("/api/leaktester/parameters", payload);
      }

      event.currentTarget.reset();
      setIsCreateModalOpen(false);
      setEditingParameter(null);
      setMessage({ kind: "ok", text: editingParameter ? "Parameter updated." : "Parameter saved." });
      await load();
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to save parameter." });
    } finally {
      setBusy(false);
    }
  }

  async function submitUpload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setUploading(true);
    setMessage(null);
    const formData = new FormData(event.currentTarget);

    try {
      const result = await apiRequest<LeakTestParameterImportResult>("/api/leaktester/parameters/import", {
        body: formData,
        method: "POST",
      });
      event.currentTarget.reset();
      setIsUploadModalOpen(false);
      setMessage({
        kind: "ok",
        text: `Excel imported. ${result.imported} new, ${result.updated} updated, ${result.skipped} skipped, ${result.channels} channels.`,
      });
      await load();
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to import parameter Excel." });
    } finally {
      setUploading(false);
    }
  }

  return (
    <>
      <div className="space-y-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="text-xl font-black text-slate-900 dark:text-white">Parameter</h1>
          <div className="flex items-center gap-2 text-sm font-semibold text-slate-400 dark:text-slate-500">
            <span>Home</span>
            <span className="text-slate-500">&gt;</span>
            <span>Master Data</span>
            <span className="text-slate-500">&gt;</span>
            <span className="text-slate-900 dark:text-white">Parameter</span>
          </div>
        </div>

        {message ? (
          <div className={`rounded-md border px-4 py-3 text-sm font-medium ${message.kind === "ok" ? "border-emerald-200 bg-emerald-50 text-emerald-700" : "border-rose-200 bg-rose-50 text-rose-700"}`}>
            {message.text}
          </div>
        ) : null}

        <DataTable
          actions={
            <div className="flex flex-wrap items-center gap-3">
              <select
                className={selectClass}
                onChange={(event) => {
                  setStatusFilter(event.target.value as ParameterStatusFilter);
                  setPage(1);
                }}
                value={statusFilter}
              >
                <option value="all">All Status</option>
                <option value="active">Active</option>
                <option value="deleted">Deleted</option>
              </select>
              <button
                className="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#21A366] px-4 text-sm font-bold text-white shadow-theme-xs transition hover:bg-[#1E8E59]"
                onClick={() => setIsUploadModalOpen(true)}
                type="button"
              >
                <svg
                  aria-hidden="true"
                  className="size-4 shrink-0"
                  fill="none"
                  viewBox="0 0 24 24"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    d="M12 15V4m0 0L8 8m4-4 4 4M5 15v3a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-3"
                    stroke="currentColor"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                  />
                </svg>
                Upload Excel
              </button>
              <CreateButton
                className="bg-brand-500 hover:bg-brand-600 focus:ring-brand-500/25"
                onClick={() => {
                  setEditingParameter(null);
                  setSelectedMachines([]);
                  setIsCreateModalOpen(true);
                }}
              />
            </div>
          }
          columns={columns}
          clearFiltersDisabled={!hasFilters}
          clearFiltersLabel="Clear parameter filter"
          data={paginatedItems}
          emptyMessage="No parameter data."
          limitOptions={PAGE_SIZE_OPTIONS}
          minWidth="1160px"
          onLimitChange={(limit) => {
            setPageSize(limit);
            setPage(1);
          }}
          onClearFilters={clearFilters}
          onPageChange={setPage}
          onSearchChange={(value) => {
            setSearchText(value);
            setPage(1);
          }}
          pagination={{
            limit: pageSize,
            page: currentPage,
            total: items.length,
            totalPage: totalPages,
          }}
          rowKey="id"
          searchPlaceholder="Search channel, model, item, value, or machine"
          searchValue={searchText}
          title="Leak Test Parameter"
        />
      </div>

      <Modal
        className="mx-4 max-w-[520px] overflow-hidden rounded-[22px] bg-slate-900 p-0 text-white shadow-2xl dark:bg-slate-900"
        isOpen={isUploadModalOpen}
        onClose={() => {
          if (!uploading) setIsUploadModalOpen(false);
        }}
        showCloseButton={false}
      >
        <form onSubmit={(event) => void submitUpload(event)}>
          <button
            aria-label="Close modal"
            className="absolute right-6 top-6 inline-flex size-11 items-center justify-center rounded-full bg-slate-100 text-slate-600 transition hover:bg-slate-200 hover:text-slate-900 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 dark:hover:text-white"
            disabled={uploading}
            onClick={() => setIsUploadModalOpen(false)}
            type="button"
          >
            <CloseIcon className="size-5" />
          </button>

          <div className="px-6 pb-2 pt-7">
            <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-400">Parameter Excel</p>
            <h2 className="mt-2 text-xl font-black text-white">Upload Parameter</h2>
          </div>

          <div className="grid gap-5 px-6 py-4">
            <label className="text-sm font-bold text-white">
              Excel File
              <input
                accept=".xlsx,.xlsm"
                className="mt-2 block w-full cursor-pointer rounded-lg border border-slate-600 bg-transparent text-sm font-medium text-slate-200 file:mr-4 file:border-0 file:bg-brand-500 file:px-4 file:py-3 file:text-sm file:font-bold file:text-white hover:file:bg-brand-600"
                name="file"
                required
                type="file"
              />
            </label>
            <div className="rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 text-xs font-semibold leading-5 text-slate-300">
              Format mengikuti file master: CHANNEL NO, MODEL PARAMETER, ITEM NAME, VALUE, lalu MACHINE NAME mulai kolom E.
            </div>
          </div>

          <div className="flex justify-end gap-3 px-6 pb-6 pt-4">
            <button
              className="h-10 rounded-lg border border-slate-600 px-5 text-sm font-bold text-white transition hover:bg-slate-800"
              disabled={uploading}
              onClick={() => setIsUploadModalOpen(false)}
              type="button"
            >
              Cancel
            </button>
            <button className="h-10 rounded-lg bg-[#21A366] px-5 text-sm font-bold text-white transition hover:bg-[#1E8E59] disabled:opacity-60" disabled={uploading} type="submit">
              {uploading ? "Uploading..." : "Import Excel"}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        className="mx-4 max-w-[720px] overflow-visible rounded-[22px] bg-white p-0 text-slate-900 shadow-2xl dark:bg-slate-900 dark:text-white"
        isOpen={isCreateModalOpen}
        onClose={() => {
          if (!busy) {
            setIsCreateModalOpen(false);
            setEditingParameter(null);
          }
        }}
        showCloseButton={false}
      >
        <form onSubmit={(event) => void submit(event)}>
          <button
            aria-label="Close modal"
            className="absolute right-6 top-6 inline-flex size-11 items-center justify-center rounded-full bg-slate-100 text-slate-600 transition hover:bg-slate-200 hover:text-slate-900 disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700 dark:hover:text-white"
            disabled={busy}
            onClick={() => {
              setIsCreateModalOpen(false);
              setEditingParameter(null);
            }}
            type="button"
          >
            <CloseIcon className="size-5" />
          </button>

          <div className="px-6 pb-2 pt-7">
            <h2 className="text-xl font-black text-slate-900 dark:text-white">{editingParameter ? "Update Parameter" : "Create Parameter"}</h2>
          </div>

          <div className="grid gap-5 px-6 pb-8 pt-4 sm:grid-cols-2">
            <label className="text-sm font-bold text-slate-700 dark:text-white">
              Channel No
              <input className={modalInputClass} defaultValue={editingParameter?.channel_no ?? ""} name="channel_no" placeholder="CH#01" required />
            </label>
            <label className="text-sm font-bold text-slate-700 dark:text-white">
              Model Parameter
              <input className={modalInputClass} defaultValue={editingParameter?.model_parameter ?? ""} name="model_parameter" placeholder="TF55, TF65, TF70" required />
            </label>
            <label className="text-sm font-bold text-slate-700 dark:text-white">
              Item Name
              <input className={modalInputClass} defaultValue={editingParameter?.item_name ?? ""} name="item_name" placeholder="Pressure Setting" required />
            </label>
            <label className="text-sm font-bold text-slate-700 dark:text-white">
              Value
              <input className={modalInputClass} defaultValue={editingParameter?.item_value ?? ""} name="item_value" placeholder="30.0 kPa" required />
            </label>
            <label className="text-sm font-bold text-slate-700 dark:text-white sm:col-span-2">
              Machine Name
              <MachineMultiSelect
                onChange={setSelectedMachines}
                options={machineOptions}
                selected={selectedMachines}
              />
            </label>
            <label className="flex items-center gap-2 text-sm font-bold text-slate-700 dark:text-white">
              <input className="h-4 w-4 rounded border-slate-300 text-brand-500 focus:ring-brand-500" defaultChecked={!editingParameter?.is_deleted} name="is_active" type="checkbox" />
              Active
            </label>
          </div>

          <div className="flex justify-end gap-3 px-6 pb-6 pt-4">
            <button
              className="h-10 rounded-lg border border-slate-300 px-5 text-sm font-bold text-slate-700 transition hover:bg-slate-100 dark:border-slate-600 dark:text-white dark:hover:bg-slate-800"
              disabled={busy}
              onClick={() => {
                setIsCreateModalOpen(false);
                setEditingParameter(null);
              }}
              type="button"
            >
              Cancel
            </button>
            <button className="h-10 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600 disabled:bg-brand-300" disabled={busy} type="submit">
              {busy ? "Saving..." : editingParameter ? "Update" : "Save"}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        className="mx-4 max-w-[720px] overflow-hidden rounded-[22px] bg-white p-0 shadow-2xl dark:bg-slate-900"
        isOpen={Boolean(selectedMachineParameter)}
        onClose={() => setSelectedMachineParameter(null)}
      >
        {selectedMachineParameter ? (
          <div>
            <div className="border-b border-slate-200 px-6 py-5 dark:border-slate-800">
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Machine Name</p>
              <h2 className="mt-2 text-xl font-black text-slate-900 dark:text-white">{selectedMachineParameter.channel_no}</h2>
              <p className="mt-1 text-sm font-semibold text-slate-500 dark:text-slate-400">
                {selectedMachineParameter.model_parameter} / {selectedMachineParameter.item_name}
              </p>
            </div>

            <div className="px-6 py-5">
              <div className="mb-4 grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                  <p className="text-xs font-bold uppercase text-slate-500 dark:text-slate-400">Value</p>
                  <p className="mt-2 text-sm font-black text-brand-600 dark:text-brand-300">{selectedMachineParameter.item_value}</p>
                </div>
                <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-800 dark:bg-slate-950">
                  <p className="text-xs font-bold uppercase text-slate-500 dark:text-slate-400">Total Machine</p>
                  <p className="mt-2 text-sm font-black text-slate-900 dark:text-white">{splitMachines(selectedMachineParameter.machine_names).length}</p>
                </div>
              </div>

              <div className="flex max-h-[360px] flex-wrap gap-2 overflow-y-auto rounded-lg border border-slate-200 bg-slate-50 p-4 dark:border-slate-800 dark:bg-slate-950">
                {splitMachines(selectedMachineParameter.machine_names).map((machine) => (
                  <span
                    className="rounded-full bg-white px-3 py-1.5 text-xs font-bold text-slate-700 shadow-sm dark:bg-slate-800 dark:text-slate-100"
                    key={machine}
                  >
                    {machine}
                  </span>
                ))}
              </div>
            </div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}
