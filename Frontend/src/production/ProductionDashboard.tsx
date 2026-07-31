"use client";

import { ApexOptions } from "apexcharts";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import ClearFilterButton from "@/components/common/ClearFilterButton";
import { ArrowRightIcon, ChevronLeftIcon } from "@/icons";
import { useTheme } from "@/context/ThemeContext";
import { apiGet } from "@/lib/api";
import type { LeakTestWorkRecord } from "./types";
import { ProductionDatePicker, todayParam } from "./ui";

const ReactApexChart = dynamic(() => import("react-apexcharts"), {
  ssr: false,
});
const PRESSURE_UNIT = "MPa";
const DEFAULT_TABLE_PAGE_SIZE = 10;
const TABLE_PAGE_SIZE_OPTIONS = [10, 25, 50, 0];

function MetricCard({
  accent,
  label,
  note,
  value,
}: {
  accent: string;
  label: string;
  note: string;
  value: React.ReactNode;
}) {
  return (
    <div className="relative overflow-hidden rounded-lg border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <span className={`absolute inset-x-0 top-0 h-1 ${accent}`} />
      <p className="text-sm font-semibold text-slate-500 dark:text-slate-400">{label}</p>
      <div className="mt-3 text-3xl font-bold tracking-tight text-slate-900 dark:text-white">{value}</div>
      <p className="mt-2 text-xs text-slate-400">{note}</p>
    </div>
  );
}

function displayDate(value: string) {
  return new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(value));
}

function displayShortDate(value: string) {
  return new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short" }).format(new Date(value));
}

function displayTime(value: string) {
  return value.split(".")[0].slice(0, 5);
}

function displayPressure(value: number) {
  return `${Number(value).toFixed(2)} ${PRESSURE_UNIT}`;
}

function toDateParam(date: Date) {
  const offset = date.getTimezoneOffset();
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 10);
}

function getLastSevenDateParams(endDate: string) {
  const end = new Date(`${endDate}T00:00:00`);
  return Array.from({ length: 7 }, (_, index) => {
    const date = new Date(end);
    date.setDate(end.getDate() - (6 - index));
    return toDateParam(date);
  });
}

function getVisiblePages(currentPage: number, totalPages: number) {
  const pageCount = Math.min(5, totalPages);
  const start = Math.min(Math.max(currentPage - 2, 1), Math.max(totalPages - pageCount + 1, 1));
  return Array.from({ length: pageCount }, (_, index) => start + index);
}

export default function ProductionDashboard() {
  const { theme } = useTheme();
  const [date, setDate] = useState(todayParam());
  const today = todayParam();
  const [records, setRecords] = useState<LeakTestWorkRecord[]>([]);
  const [weeklyRecords, setWeeklyRecords] = useState<Record<string, LeakTestWorkRecord[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tablePage, setTablePage] = useState(1);
  const [tablePageSize, setTablePageSize] = useState(DEFAULT_TABLE_PAGE_SIZE);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const weekDates = getLastSevenDateParams(date);
      const [todayRecords, weeklyResults] = await Promise.all([
        apiGet<LeakTestWorkRecord[]>(`/api/leaktester/work-records?date=${date}`),
        Promise.all(weekDates.map(async (item) => ({
          date: item,
          records: await apiGet<LeakTestWorkRecord[]>(`/api/leaktester/work-records?date=${item}`),
        }))),
      ]);
      setRecords(todayRecords);
      setWeeklyRecords(Object.fromEntries(weeklyResults.map((item) => [item.date, item.records])));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the leaktester dashboard.");
    } finally {
      setLoading(false);
    }
  }, [date]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setTablePage(1);
  }, [date]);

  const judgement = useMemo(() => {
    const ok = records.filter((item) => item.result === "OK").length;
    const ng = records.filter((item) => item.result === "NG").length;
    const total = records.length;
    const okRate = total ? (ok / total) * 100 : 0;
    const ngRate = total ? (ng / total) * 100 : 0;
    return { ng, ngRate, ok, okRate, total };
  }, [records]);
  const chartData = useMemo(() => {
    const weekDates = getLastSevenDateParams(date);
    return {
      categories: weekDates.map(displayShortDate),
      ngSeries: weekDates.map((item) => (weeklyRecords[item] ?? []).filter((record) => record.result === "NG").length),
      okSeries: weekDates.map((item) => (weeklyRecords[item] ?? []).filter((record) => record.result === "OK").length),
    };
  }, [date, weeklyRecords]);
  const chartMaxValue = useMemo(
    () => Math.max(0, ...chartData.okSeries, ...chartData.ngSeries),
    [chartData.ngSeries, chartData.okSeries]
  );
  const chartOptions = useMemo<ApexOptions>(() => ({
    colors: ["#12b76a", "#e60028"],
    chart: {
      fontFamily: "Outfit, sans-serif",
      height: 260,
      toolbar: { show: false },
      type: "bar",
    },
    dataLabels: {
      background: { enabled: false },
      dropShadow: {
        blur: 2,
        color: "#020617",
        enabled: theme === "dark",
        left: 0,
        opacity: 0.45,
        top: 1,
      },
      enabled: true,
      formatter: (value: number) => (value > 0 ? `${Math.round(value)}` : ""),
      offsetY: -22,
      style: {
        colors: [theme === "dark" ? "#f8fafc" : "#0f172a"],
        fontFamily: "Outfit, sans-serif",
        fontSize: "12px",
        fontWeight: 800,
      },
    },
    fill: { opacity: 1 },
    grid: {
      borderColor: theme === "dark" ? "#1e293b" : "#cbd5e1",
      strokeDashArray: 3,
      xaxis: {
        lines: { show: false },
      },
      yaxis: {
        lines: { show: true },
      },
    },
    legend: {
      fontFamily: "Outfit",
      horizontalAlign: "left",
      position: "top",
    },
    plotOptions: {
      bar: {
        borderRadius: 5,
        borderRadiusApplication: "end",
        columnWidth: "42%",
        dataLabels: {
          position: "top",
        },
        horizontal: false,
      },
    },
    stroke: {
      colors: ["transparent"],
      show: true,
      width: 4,
    },
    tooltip: {
      x: { show: true },
      y: {
        formatter: (value: number) => `${value} record`,
      },
    },
    xaxis: {
      axisBorder: { show: false },
      axisTicks: { show: false },
      crosshairs: {
        stroke: {
          color: theme === "dark" ? "#334155" : "#94a3b8",
          width: 1,
        },
      },
      categories: chartData.categories,
      labels: {
        rotate: -20,
        style: {
          colors: theme === "dark" ? "#cbd5e1" : "#334155",
          fontFamily: "Outfit, sans-serif",
        },
        trim: true,
      },
    },
    yaxis: {
      decimalsInFloat: 0,
      labels: {
        formatter: (value: number) => `${Math.round(value)}`,
        style: {
          colors: theme === "dark" ? "#cbd5e1" : "#334155",
          fontFamily: "Outfit, sans-serif",
        },
      },
      min: 0,
      max: chartMaxValue > 0 ? chartMaxValue + Math.max(2, Math.ceil(chartMaxValue * 0.15)) : 5,
      title: { text: undefined },
    },
  }), [chartData.categories, chartMaxValue, theme]);
  const chartSeries = useMemo(() => [
    {
      data: chartData.okSeries,
      name: "OK",
    },
    {
      data: chartData.ngSeries,
      name: "NG",
    },
  ], [chartData.ngSeries, chartData.okSeries]);
  const topNgData = useMemo(() => {
    const grouped = records.reduce<Record<string, number>>((current, record) => {
      if (record.result !== "NG") return current;
      const key = record.engine_model || "Unknown Model";
      current[key] = (current[key] ?? 0) + 1;
      return current;
    }, {});
    const items = Object.entries(grouped)
      .map(([model, total]) => ({ model, total }))
      .sort((first, second) => second.total - first.total)
      .slice(0, 5);

    return {
      categories: items.length ? items.map((item) => item.model) : ["No NG Today"],
      items,
      series: items.length ? items.map((item) => item.total) : [0],
    };
  }, [records]);
  const topNgOptions = useMemo<ApexOptions>(() => ({
    colors: ["#e60028"],
    chart: {
      fontFamily: "Outfit, sans-serif",
      height: 260,
      toolbar: { show: false },
      type: "bar",
    },
    dataLabels: {
      enabled: true,
      formatter: (value: number) => `${value}`,
      style: {
        colors: ["#ffffff"],
        fontFamily: "Outfit, sans-serif",
        fontWeight: 800,
      },
    },
    fill: { opacity: 1 },
    grid: {
      borderColor: "#e2e8f0",
      xaxis: {
        lines: { show: true },
      },
    },
    legend: { show: false },
    plotOptions: {
      bar: {
        barHeight: "58%",
        borderRadius: 5,
        borderRadiusApplication: "end",
        horizontal: true,
      },
    },
    stroke: {
      colors: ["transparent"],
      show: true,
      width: 2,
    },
    tooltip: {
      y: {
        formatter: (value: number) => `${value} NG record`,
      },
    },
    xaxis: {
      axisBorder: { show: false },
      axisTicks: { show: false },
      categories: topNgData.categories,
      labels: {
        formatter: (value: string) => `${Math.round(Number(value) || 0)}`,
        style: {
          colors: "#64748b",
          fontFamily: "Outfit, sans-serif",
        },
      },
      min: 0,
    },
    yaxis: {
      labels: {
        maxWidth: 190,
        style: {
          colors: "#64748b",
          fontFamily: "Outfit, sans-serif",
        },
      },
    },
  }), [topNgData.categories]);
  const topNgSeries = useMemo(() => [
    {
      data: topNgData.series,
      name: "NG",
    },
  ], [topNgData.series]);
  const effectiveTablePageSize = tablePageSize === 0 ? Math.max(records.length, 1) : tablePageSize;
  const totalTablePages = Math.max(1, Math.ceil(records.length / effectiveTablePageSize));
  useEffect(() => {
    setTablePage((current) => Math.min(Math.max(current, 1), totalTablePages));
  }, [totalTablePages]);
  const visibleTablePages = useMemo(() => getVisiblePages(tablePage, totalTablePages), [tablePage, totalTablePages]);
  const paginatedRecords = useMemo(() => {
    const start = (tablePage - 1) * effectiveTablePageSize;
    return records.slice(start, start + effectiveTablePageSize);
  }, [effectiveTablePageSize, records, tablePage]);
  const firstTableRecord = records.length ? (tablePage - 1) * effectiveTablePageSize + 1 : 0;
  const lastTableRecord = Math.min(tablePage * effectiveTablePageSize, records.length);

  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-brand-200 bg-brand-50 px-6 py-5 text-slate-900 shadow-sm dark:border-brand-900/60 dark:bg-slate-900 dark:text-white sm:px-7">
        <div className="flex flex-col gap-5 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-brand-700 dark:text-brand-300">PT. Yanmar Diesel Indonesia</p>
            <h1 className="mt-2 text-2xl font-semibold text-slate-950 dark:text-white sm:text-[28px]">Leaktester Work Record</h1>
            <p className="mt-2 max-w-2xl text-sm text-slate-600 dark:text-slate-300">Monitor leak test judgement, OK/NG totals, and inspection records by date.</p>
          </div>
          <div className="flex items-end gap-2">
            <ProductionDatePicker className="w-[220px] max-w-full" label="Record Date" onChange={setDate} value={date} />
            <ClearFilterButton
              disabled={date === today}
              label="Reset date filter"
              onClick={() => setDate(todayParam())}
            />
          </div>
        </div>
      </section>

      {error ? <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{error} <button className="font-bold underline" onClick={() => void load()}>Try again</button></div> : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <MetricCard
          accent="bg-brand-500"
          label="Total Work Today"
          note="Total judgement records today"
          value={loading ? "..." : judgement.total}
        />
        <MetricCard
          accent="bg-emerald-500"
          label="OK Total Today"
          note="Accepted judgement records"
          value={loading ? "..." : judgement.ok}
        />
        <MetricCard
          accent="bg-rose-500"
          label="NG Total Today"
          note="Rejected judgement records"
          value={loading ? "..." : judgement.ng}
        />
        <MetricCard
          accent="bg-amber-400"
          label="OK Rate"
          note="OK percentage today"
          value={loading ? "..." : `${judgement.okRate.toFixed(1)}%`}
        />
        <MetricCard
          accent="bg-slate-500"
          label="NG Rate"
          note="NG percentage today"
          value={loading ? "..." : `${judgement.ngRate.toFixed(1)}%`}
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.25fr_0.75fr]">
        <section className="overflow-hidden rounded-lg border border-slate-200 bg-white px-5 pt-5 shadow-sm dark:border-slate-800 dark:bg-slate-900 sm:px-6 sm:pt-6">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h2 className="text-lg font-bold text-slate-900 dark:text-white">Leak Test Judgement Chart</h2>
              <p className="mt-1 text-xs text-slate-400">OK/NG bar chart for the last 7 days ending on the selected record date.</p>
            </div>
            <Link className="text-sm font-bold text-brand-600 hover:text-brand-700" href="/work-record">Work record -&gt;</Link>
          </div>
          <div className="mt-4 max-w-full overflow-x-auto custom-scrollbar">
            <div className="min-w-[720px]">
              <ReactApexChart
                height={260}
                options={chartOptions}
                series={chartSeries}
                type="bar"
              />
            </div>
          </div>
        </section>

        <section className="overflow-hidden rounded-lg border border-slate-200 bg-white px-5 pt-5 shadow-sm dark:border-slate-800 dark:bg-slate-900 sm:px-6 sm:pt-6">
          <div>
            <h2 className="text-lg font-bold text-slate-900 dark:text-white">Top 5 Model NG Today</h2>
            <p className="mt-1 text-xs text-slate-400">Models with the highest NG judgement count on the selected date.</p>
          </div>
          <div className="mt-4 max-w-full overflow-x-auto custom-scrollbar">
            <div className="min-w-[420px]">
              <ReactApexChart
                height={260}
                options={topNgOptions}
                series={topNgSeries}
                type="bar"
              />
            </div>
          </div>
          <div className="border-t border-slate-100 pb-4 pt-3 dark:border-slate-800">
            {topNgData.items.length ? (
              <div className="space-y-2">
                {topNgData.items.map((item, index) => (
                  <div className="flex items-center justify-between gap-3 text-sm" key={item.model}>
                    <span className="min-w-0 truncate font-semibold text-slate-700 dark:text-slate-200">{index + 1}. {item.model}</span>
                    <span className="shrink-0 rounded-full bg-rose-50 px-2.5 py-1 text-xs font-black text-rose-700 dark:bg-rose-500/10 dark:text-rose-300">{item.total} NG</span>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm font-semibold text-slate-400">No NG records today.</p>
            )}
          </div>
        </section>
      </div>

      <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 dark:border-slate-800">
          <div>
            <h2 className="font-bold text-slate-900 dark:text-white">Leak Test Judgement Today</h2>
            <p className="mt-1 text-xs text-slate-400">OK/NG work records for the selected date.</p>
          </div>
          <Link className="text-sm font-bold text-brand-600 hover:text-brand-700" href="/work-record">Work record -&gt;</Link>
        </div>
        <div className="overflow-x-auto px-3 pb-3">
          <table className="leak-rounded-header-table w-full min-w-[920px] border-separate border-spacing-0 text-left text-sm">
            <thead className="bg-transparent text-[11px] uppercase tracking-wider text-white">
              <tr className="bg-transparent">
                <th className="rounded-l-lg bg-brand-500 px-5 py-3">Engine Model</th>
                <th className="bg-brand-500 px-4 py-3">Engine Number</th>
                <th className="bg-brand-500 px-4 py-3">Date / Time</th>
                <th className="bg-brand-500 px-4 py-3">Machine</th>
                <th className="bg-brand-500 px-4 py-3">Pressure Input</th>
                <th className="rounded-r-lg bg-brand-500 px-5 py-3">Judgement</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {paginatedRecords.map((record) => (
                <tr className="transition hover:bg-slate-50 dark:hover:bg-slate-800/50" key={record.id}>
                  <td className="px-5 py-4 font-bold text-slate-900 dark:text-white">{record.engine_model}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.engine_number}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{displayDate(record.check_date)} / {displayTime(record.check_time)}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{record.machine_name}</td>
                  <td className="px-4 py-4 text-slate-600 dark:text-slate-300">{displayPressure(record.pressure_input)}</td>
                  <td className="px-5 py-4">
                    <span className={`rounded-full px-3 py-1 text-xs font-black ${record.result === "OK" ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300"}`}>
                      {record.result}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!loading && !records.length ? <p className="px-5 py-12 text-center text-sm text-slate-400">No work records for this date.</p> : null}
        </div>
        {records.length ? (
          <div className="flex flex-col gap-3 border-t border-slate-100 px-5 py-4 text-sm dark:border-slate-800 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-col gap-3 text-slate-500 dark:text-slate-400 sm:flex-row sm:items-center">
              <label className="flex items-center gap-2 font-medium">
                <span>Show</span>
                <select
                  className="h-9 rounded-md border border-slate-200 bg-white px-2.5 text-sm font-bold text-slate-700 outline-none transition focus:border-brand-500 focus:ring-3 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
                  onChange={(event) => {
                    setTablePageSize(Number(event.target.value));
                    setTablePage(1);
                  }}
                  value={tablePageSize}
                >
                  {TABLE_PAGE_SIZE_OPTIONS.map((size) => (
                    <option key={size} value={size}>{size === 0 ? "All" : size}</option>
                  ))}
                </select>
                <span>entries</span>
              </label>
              <span className="font-medium">
                Showing <span className="font-bold text-slate-800 dark:text-slate-100">{firstTableRecord}-{lastTableRecord}</span> of <span className="font-bold text-slate-800 dark:text-slate-100">{records.length}</span>
              </span>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <button
                aria-label="Previous page"
                className="inline-flex size-9 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-600 transition hover:border-brand-200 hover:bg-brand-50 hover:text-brand-600 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-brand-500/10"
                disabled={tablePage === 1}
                onClick={() => setTablePage((current) => Math.max(current - 1, 1))}
                type="button"
              >
                <ChevronLeftIcon className="size-4" />
              </button>
              {visibleTablePages.map((page) => (
                <button
                  className={`inline-flex size-9 items-center justify-center rounded-md text-sm font-bold transition ${
                    tablePage === page
                      ? "bg-brand-500 text-white shadow-theme-xs"
                      : "border border-slate-200 bg-white text-slate-600 hover:border-brand-200 hover:bg-brand-50 hover:text-brand-600 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-brand-500/10"
                  }`}
                  key={page}
                  onClick={() => setTablePage(page)}
                  type="button"
                >
                  {page}
                </button>
              ))}
              <button
                aria-label="Next page"
                className="inline-flex size-9 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-600 transition hover:border-brand-200 hover:bg-brand-50 hover:text-brand-600 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-brand-500/10"
                disabled={tablePage === totalTablePages}
                onClick={() => setTablePage((current) => Math.min(current + 1, totalTablePages))}
                type="button"
              >
                <ArrowRightIcon className="size-4" />
              </button>
            </div>
          </div>
        ) : null}
      </section>
    </div>
  );
}
