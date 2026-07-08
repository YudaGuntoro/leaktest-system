"use client";

import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api";
import type { ProductionActivityLog } from "./types";
import { formatDateTime } from "./ui";

export default function ProductionHistoryPage() {
  const [items, setItems] = useState<ProductionActivityLog[]>([]);
  useEffect(() => { void apiGet<ProductionActivityLog[]>("/api/production/activity-logs").then(setItems); }, []);

  return (
    <div className="space-y-6">
      <div><p className="text-xs font-bold uppercase tracking-[0.2em] text-[#0799c9]">Traceability</p><h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Production Activity</h1></div>
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="divide-y divide-slate-100 dark:divide-slate-800">
          {items.map((item) => (
            <div className="grid gap-2 px-5 py-4 text-sm sm:grid-cols-[170px_160px_1fr_auto] sm:items-center" key={item.id}>
              <p className="font-bold text-slate-800 dark:text-white">{item.wo_number || `WO #${item.production_work_order_id}`}</p>
              <span className="w-fit rounded-full bg-cyan-50 px-2.5 py-1 text-[11px] font-bold text-cyan-700">{item.activity_type.replaceAll("_", " ")}</span>
              <div><p className="text-slate-600 dark:text-slate-300">{item.remarks || "-"}</p><p className="mt-1 text-xs text-slate-400">{item.pic_name || "System"}</p></div>
              <p className="text-xs text-slate-400">{formatDateTime(item.created_at)}</p>
            </div>
          ))}
          {!items.length ? <p className="px-5 py-12 text-center text-sm text-slate-400">No production activity yet.</p> : null}
        </div>
      </div>
    </div>
  );
}
