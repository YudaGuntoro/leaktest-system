"use client";

import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api";
import type { PicCard } from "./types";
import { formatDateTime } from "./ui";

export default function PicCardsPage() {
  const [items, setItems] = useState<PicCard[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void apiGet<PicCard[]>("/api/production/pic-cards").then(setItems).catch((err) => setError(err instanceof Error ? err.message : "Failed to load data."));
  }, []);

  return (
    <div className="space-y-6">
      <div><p className="text-xs font-bold uppercase tracking-[0.2em] text-[#0799c9]">Master Data</p><h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Operator Cards</h1><p className="mt-1 text-sm text-slate-500">Operator cards available for job scanning.</p></div>
      {error ? <div className="rounded-xl bg-rose-50 p-4 text-sm text-rose-700">{error}</div> : null}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {items.map((item) => (
          <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900" key={item.id}>
            <div className="flex items-start justify-between"><span className="flex h-12 w-12 items-center justify-center rounded-xl bg-cyan-50 text-lg font-black text-[#0799c9]">{item.full_name.slice(0, 2).toUpperCase()}</span><span className={`rounded-full px-2.5 py-1 text-xs font-bold ${item.is_active ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>{item.is_active ? "ACTIVE" : "INACTIVE"}</span></div>
            <h2 className="mt-4 font-bold text-slate-900 dark:text-white">{item.full_name}</h2>
            <p className="mt-1 text-sm text-slate-500">{item.employee_no} / {item.shift}</p>
            <div className="mt-4 rounded-xl bg-slate-50 p-3 dark:bg-slate-800"><p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Card UID</p><p className="mt-1 font-mono text-sm font-bold text-slate-700 dark:text-slate-200">{item.card_uid}</p></div>
            <p className="mt-3 text-xs text-slate-400">Last scan: {formatDateTime(item.last_scanned_at)}</p>
          </article>
        ))}
      </div>
    </div>
  );
}
