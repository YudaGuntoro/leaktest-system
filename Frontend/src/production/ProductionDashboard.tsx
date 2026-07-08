"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api";
import type { ProductionDashboardSummary } from "./types";
import { formatDateTime, ProductionDatePicker, StatusBadge, todayParam } from "./ui";

function MetricCard({ accent, label, value, note }: { accent: string; label: string; value: string | number; note: string }) {
  return (
    <div className="relative overflow-hidden rounded-lg border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <span className={`absolute inset-x-0 top-0 h-1 ${accent}`} />
      <p className="text-sm font-semibold text-slate-500 dark:text-slate-400">{label}</p>
      <p className="mt-3 text-3xl font-bold tracking-tight text-slate-900 dark:text-white">{value}</p>
      <p className="mt-2 text-xs text-slate-400">{note}</p>
    </div>
  );
}

export default function ProductionDashboard() {
  const [date, setDate] = useState(todayParam());
  const [data, setData] = useState<ProductionDashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await apiGet<ProductionDashboardSummary>(`/api/production/dashboard?date=${date}`));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the production dashboard.");
    } finally {
      setLoading(false);
    }
  }, [date]);

  useEffect(() => {
    void load();
  }, [load]);

  const goodQty = Math.max(0, (data?.actual_qty ?? 0) - (data?.reject_qty ?? 0));
  const qualityRate = data?.actual_qty ? (goodQty / data.actual_qty) * 100 : 0;
  const lineCount = useMemo(() => new Set((data?.work_orders ?? []).map((item) => item.line_code)).size, [data]);

  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-sky-200 bg-[#eaf8fd] px-6 py-5 text-slate-900 shadow-sm dark:border-sky-900/60 dark:bg-slate-900 dark:text-white sm:px-7">
        <div className="flex flex-col gap-5 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[#087ea4] dark:text-cyan-300">PT YKK AP Indonesia</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950 dark:text-white sm:text-[28px]">Production Control Monitoring System</h1>
            <p className="mt-2 max-w-2xl text-sm text-slate-600 dark:text-slate-300">Monitor daily work orders, operators, cutting lists, and finished production output.</p>
          </div>
          <ProductionDatePicker className="max-w-[220px]" label="Production Date" onChange={setDate} value={date} />
        </div>
      </section>

      {error ? <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error} <button className="font-bold underline" onClick={() => void load()}>Try again</button></div> : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard accent="bg-sky-500" label="Total Work Order" note={`${lineCount} production line`} value={loading ? "..." : data?.total_work_orders ?? 0} />
        <MetricCard accent="bg-blue-500" label="Running" note="Active work orders" value={loading ? "..." : data?.running_work_orders ?? 0} />
        <MetricCard accent="bg-amber-400" label="Waiting" note="Operator / start production" value={loading ? "..." : data?.waiting_work_orders ?? 0} />
        <MetricCard accent="bg-emerald-500" label="Finished" note="Finished today" value={loading ? "..." : data?.completed_work_orders ?? 0} />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_1.9fr]">
        <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-start justify-between">
            <div><p className="text-sm font-semibold text-slate-500 dark:text-slate-300">Finished Output</p><p className="mt-1 text-xs text-slate-400 dark:text-slate-300">Actual quantity from finished WOs</p></div>
            <span className="rounded-md bg-cyan-50 px-3 py-1 text-xs font-bold text-[#087ea4] dark:bg-cyan-500/10 dark:text-cyan-300">{qualityRate.toFixed(1)}%</span>
          </div>
          <div className="mt-7 flex items-end justify-between">
            <div><span className="text-4xl font-bold text-slate-900 dark:text-white">{data?.actual_qty.toLocaleString("en-US") ?? 0}</span><span className="ml-2 text-sm font-semibold text-slate-400 dark:text-slate-300">PCS</span></div>
          </div>
          <div className="mt-6 grid grid-cols-2 gap-3">
            <div className="rounded-md bg-emerald-50 p-4 dark:bg-emerald-500/10"><p className="text-xs font-semibold text-emerald-600">Good Quantity</p><p className="mt-1 text-xl font-bold text-emerald-700 dark:text-emerald-300">{goodQty.toLocaleString("en-US")}</p></div>
            <div className="rounded-md bg-rose-50 p-4 dark:bg-rose-500/10"><p className="text-xs font-semibold text-rose-600">Reject</p><p className="mt-1 text-xl font-bold text-rose-700 dark:text-rose-300">{data?.reject_qty.toLocaleString("en-US") ?? 0}</p></div>
          </div>
          <p className="mt-4 text-center text-xs font-semibold text-slate-400">Quality rate {qualityRate.toFixed(1)}%</p>
        </section>

        <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 dark:border-slate-800">
            <div><h2 className="font-bold text-slate-900 dark:text-white">Production Line Status</h2><p className="mt-1 text-xs text-slate-400">Work order status today</p></div>
            <Link className="text-sm font-bold text-[#0799c9] hover:text-[#087ea4]" href="/production-control">Control panel -&gt;</Link>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[960px] text-left">
              <thead className="bg-[#0799c9] text-[11px] uppercase tracking-wider text-white dark:bg-[#0799c9]"><tr><th className="rounded-tl-md px-5 py-3">Work Order</th><th className="px-4 py-3">Line / Product</th><th className="px-4 py-3">Operator</th><th className="px-4 py-3">Actual</th><th className="px-4 py-3">Started</th><th className="px-4 py-3">Finished</th><th className="rounded-tr-md px-5 py-3">Status</th></tr></thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {(data?.work_orders ?? []).map((order) => {
                  return (
                    <tr className="text-sm" key={order.id}>
                      <td className="px-5 py-4"><p className="font-bold text-slate-800 dark:text-white">{order.wo_number}</p><p className="mt-1 text-xs text-slate-400">{order.cutting_list_no}</p></td>
                      <td className="px-4 py-4"><p className="font-semibold text-slate-700 dark:text-slate-200">{order.line_code}</p><p className="mt-1 max-w-[220px] truncate text-xs text-slate-400">{order.product_name}</p></td>
                      <td className="px-4 py-4">
                        {order.operators?.length ? (
                          <div className="space-y-1">
                            {order.operators.map((operator) => (
                              <p className="text-xs font-semibold text-slate-700 dark:text-slate-200" key={operator.id}>
                                {operator.full_name} <span className="text-slate-400">/ {operator.shift}</span>
                              </p>
                            ))}
                          </div>
                        ) : (
                          <>
                            <p className="font-semibold text-slate-700 dark:text-slate-200">Not scanned</p>
                            <p className="mt-1 text-xs text-slate-400">-</p>
                          </>
                        )}
                      </td>
                      <td className="px-4 py-4">
                        {order.completed_at ? (
                          <div><p className="text-sm font-black text-slate-900 dark:text-white">{order.actual_qty.toLocaleString("en-US")}</p><p className="mt-1 text-xs text-slate-400 dark:text-slate-300">Reject {order.reject_qty.toLocaleString("en-US")}</p></div>
                        ) : (
                          <span className="text-sm font-bold text-slate-400">-</span>
                        )}
                      </td>
                      <td className="px-4 py-4 text-xs font-semibold text-slate-500">{formatDateTime(order.started_at)}</td>
                      <td className="px-4 py-4 text-xs font-semibold text-slate-500">{formatDateTime(order.completed_at)}</td>
                      <td className="px-5 py-4"><StatusBadge status={order.status} /></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            {!loading && !(data?.work_orders.length) ? <p className="px-5 py-12 text-center text-sm text-slate-400">No work orders for this date.</p> : null}
          </div>
        </section>
      </div>
    </div>
  );
}
