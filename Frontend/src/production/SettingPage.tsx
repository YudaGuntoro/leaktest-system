"use client";

import { FormEvent, useState } from "react";

type BackupSchedule = "daily" | "weekly" | "monthly";

type BackupSettings = {
  backupDbLocation: string;
  schedule: BackupSchedule;
};

const STORAGE_KEY = "yanmar-leaktester-backup-settings";
const defaultSettings: BackupSettings = {
  backupDbLocation: "",
  schedule: "daily",
};
const inputClass = "mt-2 h-12 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm font-bold text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500";
const labelClass = "text-xs font-bold uppercase text-slate-600 dark:text-slate-300";

export default function SettingPage() {
  const [settings, setSettings] = useState<BackupSettings>(() => {
    if (typeof window === "undefined") return defaultSettings;

    try {
      const stored = window.localStorage.getItem(STORAGE_KEY);
      return stored ? { ...defaultSettings, ...JSON.parse(stored) } : defaultSettings;
    } catch {
      return defaultSettings;
    }
  });
  const [message, setMessage] = useState<string | null>(null);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
    setMessage("Setting backup DB saved.");
  }

  return (
    <div className="space-y-7">
      <div>
        <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">System</p>
        <h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Setting</h1>
      </div>

      {message ? (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-300">
          {message}
        </div>
      ) : null}

      <form
        className="mx-4 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900"
        onSubmit={submit}
      >
        <div className="border-b border-slate-200 px-5 py-5 dark:border-slate-800">
          <h2 className="text-base font-bold text-slate-900 dark:text-white">Backup Database</h2>
        </div>

        <div className="grid gap-5 px-5 py-6 lg:grid-cols-[minmax(0,1fr)_300px]">
          <label className={labelClass}>
            BackupDB Location
            <input
              className={inputClass}
              onChange={(event) => {
                setSettings((current) => ({ ...current, backupDbLocation: event.target.value }));
                setMessage(null);
              }}
              placeholder="D:\\Backup\\LeakTester"
              value={settings.backupDbLocation}
            />
          </label>

          <label className={labelClass}>
            Schedule
            <select
              className={inputClass}
              onChange={(event) => {
                setSettings((current) => ({ ...current, schedule: event.target.value as BackupSchedule }));
                setMessage(null);
              }}
              value={settings.schedule}
            >
              <option value="daily">Daily</option>
              <option value="weekly">Weekly</option>
              <option value="monthly">Monthly</option>
            </select>
          </label>
        </div>

        <div className="flex justify-end border-t border-slate-200 bg-slate-50 px-5 py-4 dark:border-slate-800 dark:bg-slate-900">
          <button
            className="h-10 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white transition hover:bg-brand-600"
            type="submit"
          >
            Save Setting
          </button>
        </div>
      </form>
    </div>
  );
}
