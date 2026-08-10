"use client";

import { FormEvent, useEffect, useState } from "react";
import { ConfirmModal } from "@/components/ui/modal/ConfirmModal";
import { CopyIcon } from "@/icons";
import { fetchSystemSettings, readSystemSettings, updateSystemSettings, type BackupSchedule, type SystemSettings } from "./settings";

const inputClass = "mt-2 h-12 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm font-bold text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500";
const labelClass = "text-xs font-bold uppercase text-slate-600 dark:text-slate-300";
const backupActionClass = "inline-flex h-12 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-4 text-sm font-bold text-slate-700 transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-200 dark:hover:bg-slate-800";

export default function SettingPage() {
  const [settings, setSettings] = useState<SystemSettings>(() => readSystemSettings());
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    let ignore = false;
    void fetchSystemSettings().then((result) => {
      if (!ignore) {
        setSettings(result);
      }
    });

    return () => {
      ignore = true;
    };
  }, []);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsConfirmOpen(true);
  }

  async function confirmSave() {
    setSaving(true);
    try {
      setSettings(await updateSystemSettings(settings));
      setIsConfirmOpen(false);
      setMessage("Settings saved.");
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Failed to save settings.");
    } finally {
      setSaving(false);
    }
  }

  async function pasteBackupPath() {
    try {
      const path = await navigator.clipboard.readText();
      if (!path.trim()) {
        setMessage("Clipboard is empty. Copy a folder path first.");
        return;
      }

      setSettings((current) => ({ ...current, backupDbLocation: path.trim().replace(/^"|"$/g, "") }));
      setMessage(null);
    } catch {
      setMessage("Clipboard permission is unavailable. Paste the folder path manually.");
    }
  }

  return (
    <>
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
          <h2 className="text-base font-bold text-slate-900 dark:text-white">Unit Display</h2>
        </div>

        <div className="grid gap-5 px-5 py-6 sm:grid-cols-2">
          <label className={labelClass}>
            Pressure Unit
            <input
              className={inputClass}
              onChange={(event) => {
                setSettings((current) => ({ ...current, pressureUnit: event.target.value }));
                setMessage(null);
              }}
              placeholder="MPa"
              value={settings.pressureUnit}
            />
          </label>

          <label className={labelClass}>
            Cycle Time Unit
            <input
              className={inputClass}
              onChange={(event) => {
                setSettings((current) => ({ ...current, cycleTimeUnit: event.target.value }));
                setMessage(null);
              }}
              placeholder="s"
              value={settings.cycleTimeUnit}
            />
          </label>
        </div>

        <div className="border-y border-slate-200 px-5 py-5 dark:border-slate-800">
          <h2 className="text-base font-bold text-slate-900 dark:text-white">PLC Connection</h2>
        </div>

        <div className="grid gap-5 px-5 py-6 sm:grid-cols-2">
          <label className={labelClass}>
            PLC IP Address
            <input
              className={inputClass}
              inputMode="decimal"
              onChange={(event) => {
                setSettings((current) => ({ ...current, plcIpAddress: event.target.value }));
                setMessage(null);
              }}
              placeholder="192.168.1.10"
              value={settings.plcIpAddress}
            />
          </label>
        </div>

        <div className="border-y border-slate-200 px-5 py-5 dark:border-slate-800">
          <h2 className="text-base font-bold text-slate-900 dark:text-white">Backup Database</h2>
        </div>

        <div className="grid gap-5 px-5 py-6 lg:grid-cols-[minmax(0,1fr)_300px]">
          <div className={labelClass}>
            BackupDB Location
            <div className="mt-2 grid gap-2 xl:grid-cols-[minmax(0,1fr)_auto]">
              <input
                className="h-12 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm font-bold text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500"
                onChange={(event) => {
                  setSettings((current) => ({ ...current, backupDbLocation: event.target.value }));
                  setMessage(null);
                }}
                placeholder="D:\\Backup\\LeakTester"
                value={settings.backupDbLocation}
              />
              <button className={backupActionClass} onClick={() => void pasteBackupPath()} type="button">
                <CopyIcon className="size-5" />
                Paste Path
              </button>
            </div>
            <p className="mt-2 text-xs font-semibold normal-case text-slate-500 dark:text-slate-400">
              Copy a folder path from Explorer, then paste it here.
            </p>
          </div>

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

      <ConfirmModal
        cancelText="Cancel"
        confirmText="Yes, Save"
        isOpen={isConfirmOpen}
        isLoading={saving}
        message="Are you sure you want to save these settings? Unit display and backup configuration will be updated."
        onClose={() => setIsConfirmOpen(false)}
        onConfirm={() => void confirmSave()}
        title="Save Setting?"
      />
    </>
  );
}
