"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api";
import type { CuttingList } from "./types";
import { ProductionDatePicker, todayParam } from "./ui";

const inputClass = "h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-800 outline-none focus:border-[#0799c9] dark:border-slate-700 dark:bg-slate-900 dark:text-white";

export default function CuttingListsPage() {
  const [items, setItems] = useState<CuttingList[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  const load = useCallback(async () => setItems(await apiGet<CuttingList[]>("/api/production/cutting-lists")), []);

  useEffect(() => {
    void apiGet<CuttingList[]>("/api/production/cutting-lists")
      .then(setItems)
      .catch((err) => setMessage(err instanceof Error ? err.message : "Failed to load data."));
  }, []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    try {
      await apiPost("/api/production/cutting-lists", {
        cutting_list_no: form.get("cutting_list_no"),
        product_code: form.get("product_code"),
        product_name: form.get("product_name"),
        line_code: form.get("line_code"),
        planned_qty: 0,
        unit: "PCS",
        plan_date: form.get("plan_date"),
      });
      event.currentTarget.reset();
      setMessage("Cutting list created successfully.");
      await load();
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Failed to save.");
    }
  }

  return (
    <div className="space-y-6">
      <div><p className="text-xs font-bold uppercase tracking-[0.2em] text-[#0799c9]">Master Data</p><h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Cutting Lists</h1></div>
      {message ? <div className="rounded-xl border border-cyan-200 bg-cyan-50 px-4 py-3 text-sm text-cyan-800">{message}</div> : null}
      <form className="grid gap-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900 md:grid-cols-3 xl:grid-cols-6" onSubmit={(event) => void submit(event)}>
        <input className={inputClass} name="cutting_list_no" placeholder="No. Cutting List" required />
        <input className={inputClass} name="product_code" placeholder="Product Code" required />
        <input className={`${inputClass} xl:col-span-2`} name="product_name" placeholder="Product Name" required />
        <input className={inputClass} name="line_code" placeholder="Line" required />
        <div className="flex gap-2"><ProductionDatePicker defaultValue={todayParam()} name="plan_date" required /><button className="shrink-0 rounded-xl bg-[#0799c9] px-4 text-sm font-bold text-white" type="submit">Add</button></div>
      </form>
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="bg-[#0799c9] text-xs uppercase text-white"><tr><th className="rounded-tl-md px-5 py-3">Cutting List</th><th className="px-4 py-3">Product</th><th className="px-4 py-3">Line</th><th className="px-4 py-3">Plan Date</th><th className="rounded-tr-md px-5 py-3">Status</th></tr></thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {items.map((item) => (
                <tr key={item.id}>
                  <td className="px-5 py-4 font-bold text-slate-800 dark:text-white">{item.cutting_list_no}</td>
                  <td className="px-4 py-4"><p className="font-semibold text-slate-700 dark:text-slate-200">{item.product_name}</p><p className="text-xs text-slate-400">{item.product_code}</p></td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{item.line_code}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{new Date(item.plan_date).toLocaleDateString("en-GB")}</td>
                  <td className="px-5 py-4"><span className="rounded-full bg-cyan-50 px-2.5 py-1 text-xs font-bold text-cyan-700">{item.status.replaceAll("_", " ")}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
