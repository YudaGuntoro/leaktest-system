"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import QRCode from "qrcode";
import CreateButton from "@/components/common/CreateButton";
import DataTable, { type DataTableColumn } from "@/components/common/DataTable";
import { ConfirmModal } from "@/components/ui/modal/ConfirmModal";
import { Modal } from "@/components/ui/modal";
import { CloseIcon, DownloadIcon, TrashBinIcon } from "@/icons";
import { apiGet, apiPost, apiRequest } from "@/lib/api";
import type { Operator } from "./types";

type OperatorStatusFilter = "active" | "all" | "deleted";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];
const modalInputClass = "mt-2 h-10 w-full rounded-lg border border-slate-600 bg-transparent px-3 text-sm font-medium text-white outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20";
const selectClass = "h-10 rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm font-medium text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90";

function operatorQrPayload(operator: Operator) {
  return `OPERATOR|${operator.operator_code}|${operator.operator_name}`;
}

function sanitizeFileName(value: string) {
  return value
    .replace(/[\\/:*?"<>|]+/g, "-")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "");
}

function loadImage(src: string) {
  return new Promise<HTMLImageElement>((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = reject;
    image.src = src;
  });
}

export default function OperatorPage() {
  const [items, setItems] = useState<Operator[]>([]);
  const [busy, setBusy] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingOperator, setEditingOperator] = useState<Operator | null>(null);
  const [deletingOperator, setDeletingOperator] = useState<Operator | null>(null);
  const [selectedQrOperator, setSelectedQrOperator] = useState<Operator | null>(null);
  const [qrDataUrl, setQrDataUrl] = useState("");
  const [qrDownloading, setQrDownloading] = useState(false);
  const [message, setMessage] = useState<{ kind: "ok" | "error"; text: string } | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState<OperatorStatusFilter>("active");
  const hasFilters = Boolean(searchText.trim()) || statusFilter !== "active";

  const columns: DataTableColumn<Operator>[] = [
  {
    key: "operator_code",
    header: "Operator Code",
    render: (value) => <span className="font-bold text-slate-900 dark:text-white">{String(value || "-")}</span>,
  },
  {
    key: "operator_name",
    header: "Operator Name",
  },
  {
    key: "department",
    header: "Department",
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
    key: "qr",
    header: "Action",
    render: (_value, row) => (
      <div className="flex justify-end gap-2">
        <button
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-bold text-slate-700 transition hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
          onClick={() => {
            setEditingOperator(row);
            setIsCreateModalOpen(true);
          }}
          type="button"
        >
          Edit
        </button>
        <button
          className="rounded-lg border border-brand-200 px-3 py-1.5 text-xs font-bold text-brand-600 transition hover:bg-brand-50 dark:border-brand-500/30 dark:text-brand-300 dark:hover:bg-brand-500/10"
          onClick={() => setSelectedQrOperator(row)}
          type="button"
        >
          QR
        </button>
        {!row.is_deleted ? (
          <button
            className="inline-flex items-center gap-1.5 rounded-lg border border-rose-200 px-3 py-1.5 text-xs font-bold text-rose-600 transition hover:bg-rose-50 dark:border-rose-500/30 dark:text-rose-300 dark:hover:bg-rose-500/10"
            onClick={() => setDeletingOperator(row)}
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

  useEffect(() => {
    if (!selectedQrOperator) {
      setQrDataUrl("");
      return;
    }

    let ignore = false;
    void QRCode.toDataURL(operatorQrPayload(selectedQrOperator), {
      errorCorrectionLevel: "M",
      margin: 2,
      scale: 8,
      width: 240,
    }).then((dataUrl) => {
      if (!ignore) {
        setQrDataUrl(dataUrl);
      }
    });

    return () => {
      ignore = true;
    };
  }, [selectedQrOperator]);

  const filterQuery = useMemo(() => {
    const params = new URLSearchParams();
    const term = searchText.trim();

    if (term) params.set("search", term);
    params.set("status", statusFilter);

    return `?${params.toString()}`;
  }, [searchText, statusFilter]);

  const load = useCallback(async () => {
    try {
      setItems(await apiGet<Operator[]>(`/api/leaktester/operators${filterQuery}`));
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load operator data." });
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

  const downloadQrJpg = useCallback(async () => {
    if (!selectedQrOperator || !qrDataUrl) {
      return;
    }

    setQrDownloading(true);
    try {
      const qrImage = await loadImage(qrDataUrl);
      const canvas = document.createElement("canvas");
      const width = 900;
      const height = 1080;
      canvas.width = width;
      canvas.height = height;

      const context = canvas.getContext("2d");
      if (!context) {
        throw new Error("Canvas is not available.");
      }

      context.fillStyle = "#0f172a";
      context.fillRect(0, 0, width, height);

      context.fillStyle = "#ef0037";
      context.font = "700 28px Arial";
      context.textAlign = "center";
      context.letterSpacing = "6px";
      context.fillText("OPERATOR QR", width / 2, 92);
      context.letterSpacing = "0px";

      context.fillStyle = "#ffffff";
      context.font = "800 48px Arial";
      context.fillText(selectedQrOperator.operator_name, width / 2, 160);

      context.font = "700 34px Arial";
      context.fillText(selectedQrOperator.operator_code, width / 2, 214);

      const qrBoxSize = 620;
      const qrBoxX = (width - qrBoxSize) / 2;
      const qrBoxY = 290;
      context.fillStyle = "#ffffff";
      context.beginPath();
      context.roundRect(qrBoxX, qrBoxY, qrBoxSize, qrBoxSize, 24);
      context.fill();
      context.drawImage(qrImage, qrBoxX + 38, qrBoxY + 38, qrBoxSize - 76, qrBoxSize - 76);

      const payload = operatorQrPayload(selectedQrOperator);
      context.fillStyle = "#020617";
      context.beginPath();
      context.roundRect(72, 960, width - 144, 74, 14);
      context.fill();
      context.fillStyle = "#ffffff";
      context.font = "700 24px Arial";
      context.fillText(payload, width / 2, 1008);

      const link = document.createElement("a");
      link.href = canvas.toDataURL("image/jpeg", 0.95);
      link.download = `operator-qr-${sanitizeFileName(selectedQrOperator.operator_code)}.jpg`;
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to download operator QR." });
    } finally {
      setQrDownloading(false);
    }
  }, [qrDataUrl, selectedQrOperator]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    const form = new FormData(event.currentTarget);

    try {
      const payload = {
        operator_name: form.get("operator_name"),
        department: form.get("department"),
        note: form.get("note"),
        is_deleted: form.get("is_active") !== "on",
      };

      if (editingOperator) {
        await apiRequest<Operator>(`/api/leaktester/operators/${editingOperator.id}`, {
          body: JSON.stringify(payload),
          method: "PUT",
        });
      } else {
        await apiPost<Operator>("/api/leaktester/operators", payload);
      }

      event.currentTarget.reset();
      setIsCreateModalOpen(false);
      setEditingOperator(null);
      setMessage({ kind: "ok", text: editingOperator ? "Operator updated." : "Operator saved." });
      await load();
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to save operator." });
    } finally {
      setBusy(false);
    }
  }

  async function deleteOperator() {
    if (!deletingOperator) {
      return;
    }

    setBusy(true);
    setMessage(null);
    try {
      await apiRequest<Operator>(`/api/leaktester/operators/${deletingOperator.id}`, {
        method: "DELETE",
      });
      setDeletingOperator(null);
      setMessage({ kind: "ok", text: "Operator deleted." });
      await load();
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to delete operator." });
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <div className="space-y-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="text-xl font-black text-slate-900 dark:text-white">Operator</h1>
          <div className="flex items-center gap-2 text-sm font-semibold text-slate-400 dark:text-slate-500">
            <span>Home</span>
            <span className="text-slate-500">&gt;</span>
            <span>Master Data</span>
            <span className="text-slate-500">&gt;</span>
            <span className="text-slate-900 dark:text-white">Operator</span>
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
                  setStatusFilter(event.target.value as OperatorStatusFilter);
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
                  setEditingOperator(null);
                  setIsCreateModalOpen(true);
                }}
              />
            </div>
          }
          columns={columns}
          clearFiltersDisabled={!hasFilters}
          clearFiltersLabel="Clear operator filter"
          data={paginatedItems}
          emptyMessage="No operator data."
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
          searchPlaceholder="Search operator code, name, or department"
          searchValue={searchText}
        />
      </div>

      <Modal
        className="mx-4 max-w-[420px] overflow-hidden rounded-[22px] bg-white p-0 shadow-2xl dark:bg-slate-900"
        isOpen={Boolean(selectedQrOperator)}
        onClose={() => setSelectedQrOperator(null)}
      >
        {selectedQrOperator ? (
          <div className="p-6">
            <div className="text-center">
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">Operator QR</p>
              <h2 className="mt-2 text-xl font-black text-slate-900 dark:text-white">{selectedQrOperator.operator_name}</h2>
              <p className="mt-1 text-sm font-semibold text-slate-500 dark:text-slate-400">{selectedQrOperator.operator_code}</p>
            </div>

            <div className="mt-6 flex justify-center">
              <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
                {qrDataUrl ? (
                  <img alt={`QR ${selectedQrOperator.operator_code}`} className="h-60 w-60" src={qrDataUrl} />
                ) : (
                  <div className="flex h-60 w-60 items-center justify-center text-sm font-bold text-slate-400">Loading QR...</div>
                )}
              </div>
            </div>

            <div className="mt-5 rounded-lg bg-slate-50 px-4 py-3 text-center text-xs font-bold text-slate-600 dark:bg-slate-950 dark:text-slate-300">
              {operatorQrPayload(selectedQrOperator)}
            </div>

            <button
              className="mt-4 inline-flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-brand-500 px-4 text-sm font-black text-white shadow-sm transition hover:bg-brand-600 disabled:cursor-not-allowed disabled:bg-brand-300 disabled:opacity-70"
              disabled={!qrDataUrl || qrDownloading}
              onClick={() => void downloadQrJpg()}
              type="button"
            >
              <DownloadIcon className="size-5" />
              {qrDownloading ? "Downloading..." : "Download JPG"}
            </button>
          </div>
        ) : null}
      </Modal>

      <Modal
        className="mx-4 max-w-[500px] overflow-hidden rounded-[22px] bg-slate-900 p-0 text-white shadow-2xl dark:bg-slate-900"
        isOpen={isCreateModalOpen}
        onClose={() => {
          if (!busy) {
            setIsCreateModalOpen(false);
            setEditingOperator(null);
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
              setEditingOperator(null);
            }}
            type="button"
          >
            <CloseIcon className="size-5" />
          </button>

          <div className="px-6 pb-2 pt-7">
            <h2 className="text-xl font-black text-white">{editingOperator ? "Update Operator" : "Create Operator"}</h2>
          </div>

          <div className="grid gap-5 px-6 py-4">
            {editingOperator ? (
              <div className="text-sm font-bold text-white">
                Operator Code
                <div className="mt-2 flex h-10 w-full items-center rounded-lg border border-slate-700 bg-slate-950/70 px-3 text-sm font-black text-slate-300">
                  {editingOperator.operator_code}
                </div>
              </div>
            ) : (
              <div className="rounded-lg border border-brand-500/30 bg-brand-500/10 px-4 py-3 text-sm font-bold text-brand-100">
                Operator Code will be generated by system.
              </div>
            )}
            <label className="text-sm font-bold text-white">
              Operator Name
              <input className={modalInputClass} defaultValue={editingOperator?.operator_name ?? ""} name="operator_name" placeholder="Enter operator name" required />
            </label>
            <label className="text-sm font-bold text-white">
              Department
              <input className={modalInputClass} defaultValue={editingOperator?.department ?? ""} name="department" placeholder="Enter department" />
            </label>
            <label className="text-sm font-bold text-white">
              Note
              <textarea className={`${modalInputClass} h-24 resize-y py-3`} defaultValue={editingOperator?.note ?? ""} name="note" placeholder="Enter note" />
            </label>
            <label className="flex items-center gap-2 text-sm font-bold text-white">
              <input className="h-4 w-4 rounded border-slate-300 text-brand-500 focus:ring-brand-500" defaultChecked={!editingOperator?.is_deleted} name="is_active" type="checkbox" />
              Active
            </label>
          </div>

          <div className="flex justify-end gap-3 px-6 pb-6 pt-4">
            <button
              className="h-10 rounded-lg border border-slate-600 px-5 text-sm font-bold text-white transition hover:bg-slate-800"
              disabled={busy}
              onClick={() => {
                setIsCreateModalOpen(false);
                setEditingOperator(null);
              }}
              type="button"
            >
              Cancel
            </button>
            <button className="h-10 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600 disabled:bg-brand-300" disabled={busy} type="submit">
              {busy ? "Saving..." : editingOperator ? "Update" : "Save"}
            </button>
          </div>
        </form>
      </Modal>

      <ConfirmModal
        cancelText="Cancel"
        confirmText="Yes, Delete"
        isDestructive
        isLoading={busy}
        isOpen={Boolean(deletingOperator)}
        message={deletingOperator ? `Are you sure you want to delete operator ${deletingOperator.operator_code} - ${deletingOperator.operator_name}? This operator will be marked as deleted and hidden from active lists.` : ""}
        onClose={() => {
          if (!busy) setDeletingOperator(null);
        }}
        onConfirm={() => void deleteOperator()}
        title="Delete Operator?"
      />
    </>
  );
}
