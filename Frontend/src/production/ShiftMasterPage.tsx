"use client";

import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api";
import type { ShiftMaster } from "./types";

export default function ShiftMasterPage() {
  const [items, setItems] = useState<ShiftMaster[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void apiGet<ShiftMaster[]>("/api/production/shift-masters")
      .then(setItems)
      .catch((err) => setError(err instanceof Error ? err.message : "Failed to load data."));
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <p className="text-xs font-bold uppercase tracking-[0.2em] text-[#0799c9]">Master Data</p>
        <h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Shift Master</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-300">Production shifts available for operator scanning.</p>
      </div>

      {error ? <div className="rounded-xl bg-rose-50 p-4 text-sm text-rose-700">{error}</div> : null}

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="bg-[#0799c9] text-xs uppercase text-white">
              <tr>
                <th className="rounded-tl-md px-5 py-3">Shift</th>
                <th className="px-4 py-3">Code</th>
                <th className="px-4 py-3">Order</th>
                <th className="rounded-tr-md px-5 py-3">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {items.map((item) => (
                <tr key={item.id}>
                  <td className="px-5 py-4 font-bold text-slate-800 dark:text-white">{item.shift_name}</td>
                  <td className="px-4 py-4 font-mono text-xs font-bold text-slate-500 dark:text-slate-300">{item.shift_code}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{item.sort_order}</td>
                  <td className="px-5 py-4">
                    <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${item.is_active ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-300"}`}>
                      {item.is_active ? "ACTIVE" : "INACTIVE"}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!items.length && !error ? <p className="px-5 py-12 text-center text-sm text-slate-400 dark:text-slate-200">No shift master data.</p> : null}
        </div>
      </div>
    </div>
  );
}
