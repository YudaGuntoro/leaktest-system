"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import CreateButton from "@/components/common/CreateButton";
import DataTable, { type DataTableColumn } from "@/components/common/DataTable";
import { ConfirmModal } from "@/components/ui/modal/ConfirmModal";
import { Modal } from "@/components/ui/modal";
import { CloseIcon, TrashBinIcon } from "@/icons";
import { apiGet, apiPost, apiRequest } from "@/lib/api";
import type { EngineModel } from "./types";

type EngineModelStatusFilter = "active" | "all" | "deleted";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];
const modalInputClass = "mt-2 h-10 w-full rounded-lg border border-slate-600 bg-transparent px-3 text-sm font-medium text-white outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20";
const selectClass = "h-10 rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm font-medium text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90";

export default function EngineModelPage() {
  const [items, setItems] = useState<EngineModel[]>([]);
  const [busy, setBusy] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingEngineModel, setEditingEngineModel] = useState<EngineModel | null>(null);
  const [deletingEngineModel, setDeletingEngineModel] = useState<EngineModel | null>(null);
  const [deleteBlockedMessage, setDeleteBlockedMessage] = useState("");
  const [message, setMessage] = useState<{ kind: "ok" | "error"; text: string } | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState<EngineModelStatusFilter>("active");
  const hasFilters = Boolean(searchText.trim()) || statusFilter !== "active";

  const columns: DataTableColumn<EngineModel>[] = [
    {
      key: "engine_model",
      header: "Engine Model",
      render: (value) => <span className="font-bold text-slate-900 dark:text-white">{String(value || "-")}</span>,
    },
    {
      key: "description",
      header: "Description",
    },
    {
      key: "note",
      header: "Note",
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
        <div className="flex justify-end gap-2">
          <button
            className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-bold text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
            onClick={() => {
              setEditingEngineModel(row);
              setIsCreateModalOpen(true);
            }}
            type="button"
          >
            Update
          </button>
          {!row.is_deleted ? (
            <button
              className="inline-flex items-center gap-1.5 rounded-lg border border-rose-200 px-3 py-1.5 text-xs font-bold text-rose-600 transition hover:bg-rose-50 dark:border-rose-500/30 dark:text-rose-300 dark:hover:bg-rose-500/10"
              onClick={() => setDeletingEngineModel(row)}
              type="button"
            >
              <TrashBinIcon className="size-3.5" />
              Delete
            </button>
          ) : null}
        </div>
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
      setItems(await apiGet<EngineModel[]>(`/api/leaktester/engine-models${filterQuery}`));
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load engine model data." });
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

  const totalPages = Math.max(1, Math.ceil(items.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const paginatedItems = useMemo(() => {
    const start = (currentPage - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [currentPage, items, pageSize]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    setBusy(true);
    setMessage(null);
    const form = new FormData(formElement);

    try {
      const payload = {
        engine_model: form.get("engine_model"),
        description: form.get("description"),
        note: form.get("note"),
        is_deleted: form.get("is_active") !== "on",
      };

      if (editingEngineModel) {
        await apiRequest<EngineModel>(`/api/leaktester/engine-models/${editingEngineModel.id}`, {
          body: JSON.stringify(payload),
          method: "PUT",
        });
      } else {
        await apiPost<EngineModel>("/api/leaktester/engine-models", payload);
      }

      formElement.reset();
      setIsCreateModalOpen(false);
      setEditingEngineModel(null);
      setMessage({ kind: "ok", text: editingEngineModel ? "Engine model updated." : "Engine model saved." });
      await load();
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to save engine model." });
    } finally {
      setBusy(false);
    }
  }

  async function deleteEngineModel() {
    if (!deletingEngineModel) {
      return;
    }

    setBusy(true);
    setMessage(null);
    try {
      await apiRequest<EngineModel>(`/api/leaktester/engine-models/${deletingEngineModel.id}`, {
        method: "DELETE",
      });
      setDeletingEngineModel(null);
      setMessage({ kind: "ok", text: "Engine model deleted." });
      await load();
    } catch (err) {
      const text = err instanceof Error ? err.message : "Failed to delete engine model.";
      setDeletingEngineModel(null);
      if (text.toLowerCase().includes("leaktester work record")) {
        setDeleteBlockedMessage("Tidak bisa dihapus, karena ada data di Leaktester Work Record.");
      } else {
        setMessage({ kind: "error", text });
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <div className="space-y-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="text-xl font-black text-slate-900 dark:text-white">Engine Model</h1>
          <div className="flex items-center gap-2 text-sm font-semibold text-slate-400 dark:text-slate-500">
            <span>Home</span>
            <span className="text-slate-500">&gt;</span>
            <span>Master Data</span>
            <span className="text-slate-500">&gt;</span>
            <span className="text-slate-900 dark:text-white">Engine Model</span>
          </div>
        </div>

        {message ? (
          <div className={`rounded-md border px-4 py-3 text-sm font-medium ${message.kind === "ok" ? "border-emerald-200 bg-emerald-50 text-emerald-700" : "border-rose-200 bg-rose-50 text-rose-700"}`}>
            {message.text}
          </div>
        ) : null}

        <DataTable
          actions={
            <div className="flex items-center gap-3">
              <select
                className={selectClass}
                onChange={(event) => {
                  setStatusFilter(event.target.value as EngineModelStatusFilter);
                  setPage(1);
                }}
                value={statusFilter}
              >
                <option value="all">All Status</option>
                <option value="active">Active</option>
                <option value="deleted">Deleted</option>
              </select>
              <CreateButton
                className="bg-brand-500 hover:bg-brand-600 focus:ring-brand-500/25"
                onClick={() => {
                  setEditingEngineModel(null);
                  setIsCreateModalOpen(true);
                }}
              />
            </div>
          }
          columns={columns}
          clearFiltersDisabled={!hasFilters}
          clearFiltersLabel="Clear engine model filter"
          data={paginatedItems}
          emptyMessage="No engine model data."
          limitOptions={PAGE_SIZE_OPTIONS}
          minWidth="900px"
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
          searchPlaceholder="Search engine model or description"
          searchValue={searchText}
        />
      </div>

      <Modal
        className="mx-4 max-w-[500px] overflow-hidden rounded-[22px] bg-slate-900 p-0 text-white shadow-2xl dark:bg-slate-900"
        isOpen={isCreateModalOpen}
        onClose={() => {
          if (!busy) {
            setIsCreateModalOpen(false);
            setEditingEngineModel(null);
          }
        }}
        showCloseButton={false}
      >
        <form onSubmit={(event) => void submit(event)}>
          <button
            aria-label="Close modal"
            className="absolute right-6 top-6 inline-flex size-11 items-center justify-center rounded-full bg-slate-800 text-slate-300 transition hover:bg-slate-700 hover:text-white disabled:cursor-not-allowed disabled:opacity-60"
            disabled={busy}
            onClick={() => {
              setIsCreateModalOpen(false);
              setEditingEngineModel(null);
            }}
            type="button"
          >
            <CloseIcon className="size-5" />
          </button>

          <div className="px-6 pb-2 pt-7">
            <h2 className="text-xl font-black text-white">{editingEngineModel ? "Update Engine Model" : "Create Engine Model"}</h2>
          </div>

          <div className="grid gap-5 px-6 py-4">
            <label className="text-sm font-bold text-white">
              Engine Model
              <input className={modalInputClass} defaultValue={editingEngineModel?.engine_model ?? ""} name="engine_model" placeholder="Enter engine model" required />
            </label>
            <label className="text-sm font-bold text-white">
              Description
              <input className={modalInputClass} defaultValue={editingEngineModel?.description ?? ""} name="description" placeholder="Enter description" />
            </label>
            <label className="text-sm font-bold text-white">
              Note
              <textarea
                className={`${modalInputClass} h-24 resize-y py-3`}
                defaultValue={editingEngineModel?.note ?? ""}
                name="note"
                placeholder="Enter note"
              />
            </label>
            <label className="flex items-center gap-2 text-sm font-bold text-white">
              <input className="h-4 w-4 rounded border-slate-300 text-brand-500 focus:ring-brand-500" defaultChecked={!editingEngineModel?.is_deleted} name="is_active" type="checkbox" />
              Active
            </label>
          </div>

          <div className="flex justify-end gap-3 px-6 pb-6 pt-4">
            <button
              className="h-10 rounded-lg border border-slate-600 px-5 text-sm font-bold text-white transition hover:bg-slate-800"
              disabled={busy}
              onClick={() => {
                setIsCreateModalOpen(false);
                setEditingEngineModel(null);
              }}
              type="button"
            >
              Cancel
            </button>
            <button className="h-10 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600 disabled:bg-brand-300" disabled={busy} type="submit">
              {busy ? "Saving..." : editingEngineModel ? "Update" : "Save"}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmModal
        cancelText="Cancel"
        confirmText="Yes, Delete"
        isDestructive
        isLoading={busy}
        isOpen={Boolean(deletingEngineModel)}
        message={deletingEngineModel ? `Are you sure you want to delete engine model ${deletingEngineModel.engine_model}? This engine model will be marked as deleted and hidden from active lists.` : ""}
        onClose={() => {
          if (!busy) setDeletingEngineModel(null);
        }}
        onConfirm={() => void deleteEngineModel()}
        title="Delete Engine Model?"
      />

      <Modal
        className="mx-4 max-w-[430px] overflow-hidden rounded-[18px] bg-white p-0 shadow-2xl dark:bg-slate-900"
        isOpen={Boolean(deleteBlockedMessage)}
        onClose={() => setDeleteBlockedMessage("")}
        showCloseButton={false}
      >
        <div className="p-6">
          <h3 className="text-lg font-black text-slate-900 dark:text-white">Tidak Bisa Dihapus</h3>
          <p className="mt-2 text-sm leading-6 text-slate-600 dark:text-slate-300">{deleteBlockedMessage}</p>
          <div className="mt-6 flex justify-end">
            <button
              className="h-10 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600"
              onClick={() => setDeleteBlockedMessage("")}
              type="button"
            >
              OK
            </button>
          </div>
        </div>
      </Modal>
    </>
  );
}
