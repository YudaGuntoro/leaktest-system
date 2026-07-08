"use client";

import { FormEvent, KeyboardEvent, useCallback, useEffect, useMemo, useState } from "react";
import { apiGet, apiPost } from "@/lib/api";
import type { ProductionWorkOrder } from "./types";
import { formatDateTime, ProductionDatePicker, StatusBadge, todayParam } from "./ui";

const inputClass =
  "h-11 w-full rounded-md border border-slate-200 bg-white px-3 text-sm text-slate-800 placeholder:text-slate-500 outline-none focus:border-[#0799c9] focus:ring-2 focus:ring-cyan-500/10 disabled:cursor-not-allowed disabled:opacity-100 dark:border-slate-600 dark:bg-slate-950 dark:text-slate-50 dark:placeholder:text-slate-300 disabled:dark:text-slate-300";

const activeStatuses = new Set(["WAITING", "READY", "IN_PROGRESS", "HOLD"]);

function isActiveOrder(order: ProductionWorkOrder) {
  return activeStatuses.has(order.status);
}

function actualText(order?: ProductionWorkOrder | null) {
  return order?.completed_at ? order.actual_qty.toLocaleString("en-US") : "-";
}

function rejectText(order?: ProductionWorkOrder | null) {
  return order?.completed_at ? order.reject_qty.toLocaleString("en-US") : "-";
}

export default function ProductionControlPage() {
  const [orders, setOrders] = useState<ProductionWorkOrder[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [date, setDate] = useState(todayParam());
  const [woScanCode, setWoScanCode] = useState("");
  const [operatorCardUid, setOperatorCardUid] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ kind: "ok" | "error"; text: string } | null>(null);

  const load = useCallback(async (preferredId?: number) => {
    try {
      const workOrders = await apiGet<ProductionWorkOrder[]>(`/api/production/work-orders?date=${date}`);
      setOrders(workOrders);
      setSelectedId((current) => {
        if (preferredId && workOrders.some((order) => order.id === preferredId)) {
          return preferredId;
        }

        if (current && workOrders.some((order) => order.id === current)) {
          return current;
        }

        return workOrders.find(isActiveOrder)?.id ?? workOrders[0]?.id ?? null;
      });
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load data." });
    }
  }, [date]);

  useEffect(() => {
    void load();
  }, [load]);

  const selected = useMemo(
    () => orders.find((order) => order.id === selectedId) ?? null,
    [orders, selectedId],
  );

  const activeOrders = useMemo(() => orders.filter(isActiveOrder), [orders]);
  const visibleOrders = useMemo(() => orders.filter((order) => order.status !== "CANCELLED"), [orders]);
  const runningOrders = useMemo(
    () => activeOrders.filter((order) => order.status === "IN_PROGRESS" || order.status === "HOLD"),
    [activeOrders],
  );
  const waitingOperator = useMemo(
    () => activeOrders.filter((order) => !order.operators?.length).length,
    [activeOrders],
  );

  async function refreshFromResponse(order: ProductionWorkOrder, text: string) {
    setSelectedId(order.id);
    setMessage({ kind: "ok", text });
    await load(order.id);
  }

  async function scanWorkOrder(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const code = woScanCode.trim();
    if (!code) {
      setMessage({ kind: "error", text: "Scan the WO QR code first." });
      return;
    }

    setBusy(true);
    setMessage(null);
    try {
      const order = await apiPost<ProductionWorkOrder>("/api/production/work-orders/scan", { code });
      setWoScanCode("");
      await refreshFromResponse(order, "Active WO selected from QR scan.");
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to scan WO." });
    } finally {
      setBusy(false);
    }
  }

  async function scanOperator(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) {
      setMessage({ kind: "error", text: "Select an active WO first." });
      return;
    }

    const cardUid = operatorCardUid.trim();
    if (!cardUid) {
      setMessage({ kind: "error", text: "Scan the operator ID first." });
      return;
    }

    setBusy(true);
    setMessage(null);
    try {
      const order = await apiPost<ProductionWorkOrder>(`/api/production/work-orders/${selected.id}/scan-pic`, { card_uid: cardUid });
      setOperatorCardUid("");
      await refreshFromResponse(order, "Operator added successfully.");
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to scan operator." });
    } finally {
      setBusy(false);
    }
  }

  async function removeOperator(operatorId: number) {
    if (!selected) return;
    setBusy(true);
    setMessage(null);
    try {
      const order = await apiPost<ProductionWorkOrder>(`/api/production/work-orders/${selected.id}/operators/${operatorId}/remove`);
      await refreshFromResponse(order, "Operator removed successfully.");
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to remove operator." });
    } finally {
      setBusy(false);
    }
  }

  async function action(path: string, body?: unknown, success = "Updated successfully.") {
    if (!selected) return;
    setBusy(true);
    setMessage(null);
    try {
      const order = await apiPost<ProductionWorkOrder>(`/api/production/work-orders/${selected.id}/${path}`, body);
      await refreshFromResponse(order, success);
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Process failed." });
    } finally {
      setBusy(false);
    }
  }

  function selectOrder(orderId: number) {
    setSelectedId(orderId);
  }

  function selectOrderFromKeyboard(event: KeyboardEvent<HTMLTableRowElement>, orderId: number) {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      selectOrder(orderId);
    }
  }

  const canScanOperator = Boolean(selected && selected.status !== "COMPLETED" && selected.status !== "CANCELLED");
  const selectedOperators = selected?.operators ?? [];
  const canStart = selected?.status === "READY" && selectedOperators.length > 0;
  const canFinish = selected?.status === "IN_PROGRESS";
  const canCancelFinish = selected?.status === "COMPLETED" && Boolean(selected.completed_at);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-[#0799c9]">Operation</p>
          <h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Production Control Panel</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-300">Scan WO QR codes, scan shift operator IDs, then control parallel active WOs.</p>
        </div>
        <ProductionDatePicker className="max-w-[210px]" onChange={setDate} value={date} />
      </div>

      {message ? (
        <div className={`rounded-md border px-4 py-3 text-sm font-medium ${message.kind === "ok" ? "border-emerald-200 bg-emerald-50 text-emerald-700" : "border-rose-200 bg-rose-50 text-rose-700"}`}>
          {message.text}
        </div>
      ) : null}

      <section className="rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="grid gap-0 divide-y divide-slate-100 dark:divide-slate-800 xl:grid-cols-[1fr_1fr_0.8fr] xl:divide-x xl:divide-y-0">
          <form className="p-5" onSubmit={(event) => void scanWorkOrder(event)}>
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-300">Scan QR WO / Cutting List</label>
            <div className="mt-3 flex flex-col gap-3 sm:flex-row">
              <input
                autoFocus
                className={inputClass}
                onChange={(event) => setWoScanCode(event.target.value)}
                placeholder="WO-YKK-001 or CL-YKK-001"
                value={woScanCode}
              />
              <button className="h-11 rounded-md bg-[#0799c9] px-5 text-sm font-bold text-white transition hover:bg-[#087ea4] disabled:bg-[#0f5f78] disabled:text-cyan-50 disabled:opacity-100" disabled={busy} type="submit">
                Scan WO
              </button>
            </div>
          </form>

          <form className="p-5" onSubmit={(event) => void scanOperator(event)}>
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-300">Scan ID Operator Shift</label>
            <div className="mt-3 flex flex-col gap-3 sm:flex-row">
              <input
                className={inputClass}
                disabled={!canScanOperator}
                onChange={(event) => setOperatorCardUid(event.target.value)}
                placeholder="Scan card UID"
                value={operatorCardUid}
              />
              <button className="h-11 rounded-md bg-[#0799c9] px-5 text-sm font-bold text-white transition hover:bg-[#087ea4] disabled:bg-[#0f5f78] disabled:text-cyan-50 disabled:opacity-100" disabled={!canScanOperator || busy} type="submit">
                Scan Operator
              </button>
            </div>
          </form>

          <div className="grid grid-cols-3 divide-x divide-slate-100 dark:divide-slate-800">
            <div className="p-5">
              <p className="text-xs font-semibold text-slate-500 dark:text-slate-300">Active WOs</p>
              <p className="mt-2 text-2xl font-black text-slate-900 dark:text-white">{activeOrders.length}</p>
            </div>
            <div className="p-5">
              <p className="text-xs font-semibold text-slate-500 dark:text-slate-300">Running in Parallel</p>
              <p className="mt-2 text-2xl font-black text-slate-900 dark:text-white">{runningOrders.length}</p>
            </div>
            <div className="p-5">
              <p className="text-xs font-semibold text-slate-500 dark:text-slate-300">No Operator</p>
              <p className="mt-2 text-2xl font-black text-slate-900 dark:text-white">{waitingOperator}</p>
            </div>
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 dark:border-slate-800">
          <div>
            <h2 className="font-bold text-slate-900 dark:text-white">Active WOs Today</h2>
            <p className="mt-1 text-xs text-slate-400 dark:text-slate-300">Each WO represents one Cutting List on the same production date.</p>
          </div>
          <span className="rounded-md bg-cyan-50 px-3 py-1 text-xs font-bold text-[#087ea4] dark:bg-cyan-500/10 dark:text-cyan-300">{date}</span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[1080px] border-separate border-spacing-0 text-left">
            <thead className="bg-[#0799c9] text-[11px] uppercase tracking-wider text-white">
              <tr>
                <th className="rounded-tl-md px-5 py-3">WO / Cutting List</th>
                <th className="px-4 py-3">Product</th>
                <th className="px-4 py-3">Operator Shift</th>
                <th className="px-4 py-3">Actual</th>
                <th className="px-4 py-3">Started</th>
                <th className="px-4 py-3">Finished</th>
                <th className="px-4 py-3">Status</th>
                <th className="rounded-tr-md px-5 py-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {visibleOrders.map((order) => {
                const selectedRow = selectedId === order.id;
                return (
                  <tr
                    aria-selected={selectedRow}
                    className={`cursor-pointer text-sm outline-none transition focus-visible:bg-cyan-50/70 dark:focus-visible:bg-cyan-500/10 ${selectedRow ? "bg-cyan-50/70 dark:bg-cyan-500/10" : "hover:bg-slate-50 dark:hover:bg-slate-800/50"}`}
                    key={order.id}
                    onClick={() => selectOrder(order.id)}
                    onKeyDown={(event) => selectOrderFromKeyboard(event, order.id)}
                    tabIndex={0}
                  >
                    <td className="px-5 py-4">
                      <p className="font-bold text-slate-900 dark:text-white">{order.wo_number}</p>
                      <p className="mt-1 text-xs text-slate-400 dark:text-slate-300">{order.cutting_list_no}</p>
                    </td>
                    <td className="px-4 py-4">
                      <p className="font-semibold text-slate-700 dark:text-slate-200">{order.product_name}</p>
                      <p className="mt-1 text-xs text-slate-400 dark:text-slate-300">{order.line_code} / {order.product_code}</p>
                    </td>
                    <td className="px-4 py-4">
                      {order.operators?.length ? (
                        <div className="flex max-w-[260px] flex-wrap gap-1.5">
                          {order.operators.map((operator) => (
                            <span className="rounded-md bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-700 dark:bg-slate-800 dark:text-slate-200" key={operator.id}>
                              {operator.full_name} / {operator.shift}
                            </span>
                          ))}
                        </div>
                      ) : (
                        <>
                          <p className="font-semibold text-slate-700 dark:text-slate-200">Not scanned</p>
                          <p className="mt-1 text-xs text-slate-400 dark:text-slate-300">-</p>
                        </>
                      )}
                    </td>
                    <td className="px-4 py-4">
                      {order.completed_at ? (
                        <div>
                          <p className="text-sm font-black text-slate-900 dark:text-white">{order.actual_qty.toLocaleString("en-US")}</p>
                          <p className="mt-1 text-xs text-slate-400 dark:text-slate-300">Reject {order.reject_qty}</p>
                        </div>
                      ) : (
                        <p className="text-sm font-bold text-slate-400 dark:text-slate-200">-</p>
                      )}
                    </td>
                    <td className="px-4 py-4 text-xs font-semibold text-slate-500 dark:text-slate-300">{formatDateTime(order.started_at)}</td>
                    <td className="px-4 py-4 text-xs font-semibold text-slate-500 dark:text-slate-300">{formatDateTime(order.completed_at)}</td>
                    <td className="px-4 py-4"><StatusBadge status={order.status} /></td>
                    <td className="px-5 py-4 text-right">
                      <button className="h-9 rounded-md border border-slate-200 px-3 text-xs font-bold text-slate-700 hover:border-[#0799c9] hover:text-[#0799c9] disabled:opacity-40 dark:border-slate-700 dark:text-slate-200" disabled={selectedRow} onClick={(event) => { event.stopPropagation(); selectOrder(order.id); }} type="button">
                        {selectedRow ? "Open" : "Detail"}
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {!visibleOrders.length ? <p className="px-5 py-12 text-center text-sm text-slate-400 dark:text-slate-200">No active WOs yet. Scan a WO QR code to start.</p> : null}
        </div>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex flex-col gap-3 border-b border-slate-100 pb-5 dark:border-slate-800 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-bold text-slate-900 dark:text-white">Control {selected?.wo_number || "-"}</h2>
            <p className="mt-1 text-xs text-slate-400 dark:text-slate-300">{selected ? `${selected.cutting_list_no} / ${selected.product_name}` : "Select an active WO from the table."}</p>
          </div>
          {selected ? <StatusBadge status={selected.status} /> : null}
        </div>

        {selected ? (
          <div className="mt-5 grid gap-4 xl:grid-cols-[1.25fr_1fr]">
            <div className="rounded-md border border-slate-200 p-4 dark:border-slate-700">
              <p className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-300">Detail Work Order</p>
              <div className="mt-4 grid gap-4 text-sm sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-xs text-slate-400 dark:text-slate-300">WO</p>
                  <p className="mt-1 font-black text-slate-900 dark:text-white">{selected.wo_number}</p>
                </div>
                <div>
                  <p className="text-xs text-slate-400 dark:text-slate-300">Cutting List</p>
                  <p className="mt-1 font-black text-slate-900 dark:text-white">{selected.cutting_list_no}</p>
                </div>
                <div>
                  <p className="text-xs text-slate-400 dark:text-slate-300">Line</p>
                  <p className="mt-1 font-black text-slate-900 dark:text-white">{selected.line_code}</p>
                </div>
                <div>
                  <p className="text-xs text-slate-400 dark:text-slate-300">Product</p>
                  <p className="mt-1 font-black text-slate-900 dark:text-white">{selected.product_name}</p>
                  <p className="mt-0.5 text-xs text-slate-400 dark:text-slate-300">{selected.product_code}</p>
                </div>
                <div>
                  <p className="text-xs text-slate-400 dark:text-slate-300">Actual</p>
                  <p className="mt-1 font-black text-slate-900 dark:text-white">{actualText(selected)}</p>
                </div>
              </div>
            </div>

            <div className="rounded-md border border-slate-200 p-4 dark:border-slate-700">
              <p className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-300">Assigned Operators</p>
              <div className="mt-3 space-y-2">
                {selectedOperators.length ? selectedOperators.map((operator) => (
                  <div className="rounded-md bg-slate-50 px-3 py-2 dark:bg-slate-800" key={operator.id}>
                    <div className="flex items-center justify-between gap-3">
                      <p className="truncate text-sm font-black text-slate-900 dark:text-white">{operator.full_name}</p>
                      <span className="shrink-0 rounded-md bg-white px-2 py-1 text-[11px] font-bold text-[#087ea4] dark:bg-slate-900 dark:text-cyan-300">{operator.shift}</span>
                    </div>
                    <p className="mt-1 text-xs text-slate-500 dark:text-slate-300">{operator.employee_no} / {operator.department}</p>
                    <p className="mt-1 text-xs text-slate-400 dark:text-slate-300">Scan {formatDateTime(operator.scanned_at)}</p>
                  </div>
                )) : (
                  <p className="rounded-md bg-slate-50 px-3 py-4 text-sm font-semibold text-slate-400 dark:bg-slate-800 dark:text-slate-200">No active operators.</p>
                )}
              </div>
            </div>
          </div>
        ) : null}

        <div className="mt-5 grid gap-4 lg:grid-cols-[1fr_1fr_1.2fr]">
          <div className="rounded-md border border-slate-200 p-4 dark:border-slate-700">
            <p className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-300">Operator</p>
            <div className="mt-3 space-y-2">
              {selectedOperators.length ? selectedOperators.map((operator) => (
                <div className="flex items-center justify-between gap-3 rounded-md bg-slate-50 px-3 py-2 dark:bg-slate-800" key={operator.id}>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-black text-slate-900 dark:text-white">{operator.full_name}</p>
                    <p className="mt-0.5 text-xs text-slate-500 dark:text-slate-300">{operator.employee_no} / {operator.shift}</p>
                  </div>
                  <button
                    className="h-8 shrink-0 rounded-md border border-rose-200 px-2 text-xs font-bold text-rose-600 hover:bg-rose-50 disabled:opacity-40 dark:border-rose-500/30 dark:text-rose-300"
                    disabled={!canScanOperator || busy}
                    onClick={() => void removeOperator(operator.id)}
                    type="button"
                  >
                    Remove
                  </button>
                </div>
              )) : (
                <p className="rounded-md bg-slate-50 px-3 py-4 text-sm font-semibold text-slate-400 dark:bg-slate-800 dark:text-slate-200">No active operators.</p>
              )}
            </div>
            <button className="mt-5 h-10 w-full rounded-md bg-[#0799c9] text-xs font-bold text-white hover:bg-[#087ea4] disabled:bg-[#0f5f78] disabled:text-cyan-50 disabled:opacity-100" disabled={!canStart || busy} onClick={() => void action("start", undefined, "Start timestamp saved.")} type="button">
              Start
            </button>
          </div>

          <div className="rounded-md border border-slate-200 p-4 dark:border-slate-700">
            <p className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-300">Timestamp</p>
            <div className="mt-4 grid gap-3 text-sm">
              <div className="rounded-md bg-slate-50 px-3 py-2 dark:bg-slate-800">
                <p className="text-xs text-slate-400 dark:text-slate-300">Started</p>
                <p className="mt-1 font-black text-slate-900 dark:text-white">{formatDateTime(selected?.started_at)}</p>
              </div>
              <div className="rounded-md bg-slate-50 px-3 py-2 dark:bg-slate-800">
                <p className="text-xs text-slate-400 dark:text-slate-300">Finished</p>
                <p className="mt-1 font-black text-slate-900 dark:text-white">{formatDateTime(selected?.completed_at)}</p>
              </div>
            </div>
            <div className="mt-4 grid grid-cols-2 gap-3">
              <button className="h-10 rounded-md bg-emerald-600 text-xs font-bold text-white hover:bg-emerald-700 disabled:bg-emerald-900 disabled:text-emerald-50 disabled:opacity-100" disabled={!canFinish || busy} onClick={() => void action("finish", undefined, "Finish timestamp saved.")} type="button">
                Finish
              </button>
              <button className="h-10 rounded-md border border-amber-300 text-xs font-bold text-amber-700 hover:bg-amber-50 disabled:opacity-75 dark:border-amber-500/40 dark:text-amber-200 disabled:dark:text-amber-200" disabled={!canCancelFinish || busy} onClick={() => void action("cancel-finish", undefined, "Finish canceled.")} type="button">
                Cancel Finish
              </button>
            </div>
          </div>

          <div className="rounded-md border border-slate-200 p-4 dark:border-slate-700">
            <p className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-300">Actual</p>
            <div className="mt-4 grid grid-cols-2 gap-3 text-sm">
              <div>
                <p className="text-xs text-slate-400 dark:text-slate-300">Actual</p>
                <p className="mt-1 font-black text-slate-900 dark:text-white">{actualText(selected)}</p>
              </div>
              <div>
                <p className="text-xs text-slate-400 dark:text-slate-300">Reject</p>
                <p className="mt-1 font-black text-slate-900 dark:text-white">{rejectText(selected)}</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
