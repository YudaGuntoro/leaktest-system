"use client";

import { FormEvent, useEffect, useState } from "react";
import { ConfirmModal } from "@/components/ui/modal/ConfirmModal";
import { CheckLineIcon, CopyIcon } from "@/icons";
import { apiGet, apiRequest } from "@/lib/api";
import { fetchSystemSettings, readSystemSettings, updateSystemSettings, type BackupSchedule, type SystemSettings } from "./settings";
import type { LeakTestJudgement } from "./types";

const inputClass = "mt-2 h-12 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm font-bold text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500";
const labelClass = "text-xs font-bold uppercase text-slate-600 dark:text-slate-300";
const backupActionClass = "inline-flex h-12 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-4 text-sm font-bold text-slate-700 transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-200 dark:hover:bg-slate-800";
const tableInputClass = "h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm font-bold text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:ring-3 focus:ring-brand-500/20 dark:border-slate-700 dark:bg-slate-950 dark:text-white dark:placeholder:text-slate-500";
type PageMessage = { kind: "ok" | "error"; text: string };
type RejectedMqttMessage = {
  id: number;
  topic: string;
  payload: string;
  error_message: string;
  status: string;
  attempt_count: number;
  received_at: string;
  last_attempt_at: string | null;
  pressureInput: string;
  pressSetUp: string;
  pressSetLow: string;
};
const showJudgementMaster = true;

function editPressureValues(row: RejectedMqttMessage) {
  try {
    const root = JSON.parse(row.payload) as Record<string, unknown>;
    const inputAliases = ["pressure_input", "pressureInput", "PressureInput", "press_input", "press_in_result"];
    const data = inputAliases.some((key) => key in root)
      ? root
      : Object.values(root).find((value) => value && typeof value === "object" && !Array.isArray(value) && inputAliases.some((key) => key in (value as Record<string, unknown>))) as Record<string, unknown> | undefined ?? root;
    const readValue = (names: string[]) => {
      const found = names.find((key) => key in data);
      return found ? String(data[found] ?? "") : "";
    };
    return {
      ...row,
      pressureInput: readValue(inputAliases),
      pressSetUp: readValue(["press_set_up", "pressSetUp", "PressSetUp", "upper_press_limit", "UpperPressLimit", "tp_ul", "TP_UL", "par_press_up"]),
      pressSetLow: readValue(["press_set_low", "pressSetLow", "PressSetLow", "lower_press_limit", "LowerPressLimit", "tp_ll", "TP_LL", "par_press_low"]),
    };
  } catch {
    return { ...row, pressureInput: "", pressSetUp: "", pressSetLow: "" };
  }
}

function buildEditedPayload(row: RejectedMqttMessage) {
  try {
    const root = JSON.parse(row.payload) as Record<string, unknown>;
    const aliases = ["pressure_input", "pressureInput", "PressureInput", "press_input", "press_in_result"];
    let data = aliases.some((key) => key in root)
      ? root
      : Object.values(root).find((value) => value && typeof value === "object" && !Array.isArray(value) && aliases.some((key) => key in (value as Record<string, unknown>))) as Record<string, unknown> | undefined;
    if (!data) data = root;
    data.pressure_input = row.pressureInput;
    data.press_set_up = row.pressSetUp;
    data.press_set_low = row.pressSetLow;
    return JSON.stringify(root);
  } catch {
    return row.payload;
  }
}

export default function SettingPage() {
  const [settings, setSettings] = useState<SystemSettings>(() => readSystemSettings());
  const [judgements, setJudgements] = useState<LeakTestJudgement[]>([]);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [loadingJudgements, setLoadingJudgements] = useState(false);
  const [savingJudgementId, setSavingJudgementId] = useState<number | null>(null);
  const [rejectedMessages, setRejectedMessages] = useState<RejectedMqttMessage[]>([]);
  const [loadingRejected, setLoadingRejected] = useState(false);
  const [showRejectedList, setShowRejectedList] = useState(false);
  const [reprocessingAll, setReprocessingAll] = useState(false);
  const [reprocessStartedAt, setReprocessStartedAt] = useState<number | null>(null);
  const [reprocessElapsedSeconds, setReprocessElapsedSeconds] = useState(0);
  const [message, setMessage] = useState<PageMessage | null>(null);

  useEffect(() => {
    if (reprocessStartedAt === null) return;
    const updateElapsed = () => setReprocessElapsedSeconds(Math.floor((Date.now() - reprocessStartedAt) / 1000));
    updateElapsed();
    const timer = window.setInterval(updateElapsed, 1000);
    return () => window.clearInterval(timer);
  }, [reprocessStartedAt]);

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

  async function loadRejectedMessages() {
    setLoadingRejected(true);
    try {
      const rows = await apiGet<Omit<RejectedMqttMessage, "pressureInput" | "pressSetUp" | "pressSetLow">[]>("/api/leaktester/rejected-mqtt-messages");
      setRejectedMessages(rows.map((row) => editPressureValues(row as RejectedMqttMessage)));
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load rejected MQTT messages." });
    } finally {
      setLoadingRejected(false);
    }
  }

  useEffect(() => {
    void loadRejectedMessages();
  }, []);

  async function reprocessAllRejectedMessages() {
    const pendingMessages = rejectedMessages.filter((row) => row.status.toLowerCase() === "pending");
    if (pendingMessages.length === 0) return;
    setReprocessingAll(true);
    setReprocessElapsedSeconds(0);
    setReprocessStartedAt(Date.now());
    setMessage(null);
    try {
      const result = await apiRequest<{
        published_ids: number[];
        failed_ids: number[];
        skipped_ids: number[];
        failure_reasons: { id: number; error: string }[];
      }>("/api/leaktester/rejected-mqtt-messages/reprocess-all", {
        method: "POST",
        body: JSON.stringify({ items: pendingMessages.map((row) => ({ id: row.id, payload: buildEditedPayload(row) })) }),
      });
      const failureIds = result.failed_ids.join(", ");
      setMessage({
        kind: result.failed_ids.length === 0 ? "ok" : "error",
        text: `${result.published_ids.length} payload terkirim ke MQTT; ${result.failed_ids.length} gagal dikirim; ${result.skipped_ids.length} dilewati karena sudah Published.${failureIds ? ` ID gagal: ${failureIds}.` : ""} Alasan gagal ditampilkan pada kolom Error.`,
      });
      window.setTimeout(() => void loadRejectedMessages(), 1000);
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to reprocess rejected MQTT messages." });
    } finally {
      setReprocessingAll(false);
      setReprocessStartedAt(null);
    }
  }

  useEffect(() => {
    if (!showJudgementMaster) {
      return;
    }

    let ignore = false;

    async function loadJudgements() {
      setLoadingJudgements(true);
      try {
        const rows = await apiGet<LeakTestJudgement[]>("/api/leaktester/judgements");
        if (!ignore) {
          setJudgements(rows);
        }
      } catch (err) {
        if (!ignore) {
          setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to load judgement master." });
        }
      } finally {
        if (!ignore) {
          setLoadingJudgements(false);
        }
      }
    }

    void loadJudgements();

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
      setMessage({ kind: "ok", text: "Settings saved." });
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to save settings." });
    } finally {
      setSaving(false);
    }
  }

  function updateJudgementDraft(id: number, patch: Partial<Pick<LeakTestJudgement, "judgement_name" | "result" | "note">>) {
    setJudgements((current) => current.map((item) => (item.id === id ? { ...item, ...patch } : item)));
    setMessage(null);
  }

  async function saveJudgement(item: LeakTestJudgement) {
    setSavingJudgementId(item.id);
    setMessage(null);

    try {
      const updated = await apiRequest<LeakTestJudgement>(`/api/leaktester/judgements/${item.id}`, {
        body: JSON.stringify({
          judgement_name: item.judgement_name,
          result: item.result,
          note: item.note ?? "",
          is_deleted: item.is_deleted ?? false,
        }),
        method: "PUT",
      });

      setJudgements((current) => current.map((row) => (row.id === item.id ? updated : row)));
      setMessage({ kind: "ok", text: "Judgement master saved." });
    } catch (err) {
      setMessage({ kind: "error", text: err instanceof Error ? err.message : "Failed to save judgement master." });
    } finally {
      setSavingJudgementId(null);
    }
  }

  async function pasteBackupPath() {
    try {
      const path = await navigator.clipboard.readText();
      if (!path.trim()) {
        setMessage({ kind: "error", text: "Clipboard is empty. Copy a folder path first." });
        return;
      }

      setSettings((current) => ({ ...current, backupDbLocation: path.trim().replace(/^"|"$/g, "") }));
      setMessage(null);
    } catch {
      setMessage({ kind: "error", text: "Clipboard permission is unavailable. Paste the folder path manually." });
    }
  }

  return (
    <>
      <div className="space-y-7">
      <div>
        <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-600">System</p>
        <h1 className="mt-2 text-2xl font-black text-slate-900 dark:text-white">Setting</h1>
        <button
          className="mt-4 inline-flex h-11 items-center gap-2 rounded-lg border border-brand-200 bg-brand-50 px-4 text-sm font-bold text-brand-700 transition hover:bg-brand-100 dark:border-brand-500/20 dark:bg-brand-500/10 dark:text-brand-300 dark:hover:bg-brand-500/20"
          onClick={() => {
            setShowRejectedList(true);
            void loadRejectedMessages();
          }}
          type="button"
        >
          Unprocessed Data ({rejectedMessages.length})
        </button>
      </div>

      {message ? (
        <div
          className={
            message.kind === "ok"
              ? "rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-300"
              : "rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700 dark:border-red-500/20 dark:bg-red-500/10 dark:text-red-300"
          }
        >
          {message.text}
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

      {showJudgementMaster ? (
      <section className="mx-4 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="border-b border-slate-200 px-5 py-5 dark:border-slate-800">
          <h2 className="text-base font-bold text-slate-900 dark:text-white">Judgement Master</h2>
        </div>

        <div className="overflow-x-auto p-5">
          <table className="leak-rounded-header-table w-full min-w-[760px] border-separate border-spacing-0 text-left text-sm">
            <thead className="bg-transparent text-xs uppercase text-white">
              <tr>
                <th className="w-32 rounded-l-lg bg-brand-500 px-5 py-3 text-center">Code</th>
                <th className="bg-brand-500 px-4 py-3">Judgement Name</th>
                <th className="w-36 bg-brand-500 px-4 py-3">Result</th>
                <th className="bg-brand-500 px-4 py-3">Note</th>
                <th className="w-32 rounded-r-lg bg-brand-500 px-5 py-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody>
              {loadingJudgements ? (
                <tr>
                  <td className="px-3 py-5 text-sm font-semibold text-slate-500 dark:text-slate-400" colSpan={5}>
                    Loading...
                  </td>
                </tr>
              ) : null}

              {!loadingJudgements && judgements.length === 0 ? (
                <tr>
                  <td className="px-3 py-5 text-sm font-semibold text-slate-500 dark:text-slate-400" colSpan={5}>
                    No judgement data.
                  </td>
                </tr>
              ) : null}

              {judgements.map((item) => (
                <tr key={item.id}>
                  <td className="border-b border-slate-100 px-3 py-3 text-center font-black text-slate-900 dark:border-slate-800 dark:text-white">
                    {item.judgement_code}
                  </td>
                  <td className="border-b border-slate-100 px-3 py-3 dark:border-slate-800">
                    <input
                      className={tableInputClass}
                      onChange={(event) => updateJudgementDraft(item.id, { judgement_name: event.target.value })}
                      value={item.judgement_name}
                    />
                  </td>
                  <td className="border-b border-slate-100 px-3 py-3 dark:border-slate-800">
                    <select
                      className={tableInputClass}
                      onChange={(event) => updateJudgementDraft(item.id, { result: event.target.value as LeakTestJudgement["result"] })}
                      value={item.result ?? ""}
                    >
                      <option value=""></option>
                      <option value="OK">OK</option>
                      <option value="NG">NG</option>
                    </select>
                  </td>
                  <td className="border-b border-slate-100 px-3 py-3 dark:border-slate-800">
                    <input
                      className={tableInputClass}
                      onChange={(event) => updateJudgementDraft(item.id, { note: event.target.value })}
                      value={item.note ?? ""}
                    />
                  </td>
                  <td className="border-b border-slate-100 px-3 py-3 text-right dark:border-slate-800">
                    <button
                      className="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-brand-500 px-4 text-sm font-bold text-white transition hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-60"
                      disabled={savingJudgementId === item.id}
                      onClick={() => void saveJudgement(item)}
                      type="button"
                    >
                      <CheckLineIcon className="size-4" />
                      {savingJudgementId === item.id ? "Saving" : "Save"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      ) : null}

      {showRejectedList ? (
      <div
        className="fixed inset-0 z-[100000] flex items-center justify-center bg-slate-950/60 p-3 sm:p-6"
        role="presentation"
        onMouseDown={(event) => { if (event.target === event.currentTarget) setShowRejectedList(false); }}
      >
      <section aria-labelledby="unprocessed-data-title" aria-modal="true" className="flex max-h-[92vh] w-full max-w-[96vw] flex-col overflow-hidden rounded-xl border border-slate-200 bg-white shadow-2xl dark:border-slate-800 dark:bg-slate-900" role="dialog">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-5 py-5 dark:border-slate-800">
          <div>
            <h2 id="unprocessed-data-title" className="text-base font-bold text-slate-900 dark:text-white">Unprocessed MQTT Data</h2>
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">Hanya data berstatus Pending yang dikirim. Data Published tidak dikirim ulang.</p>
          </div>
          <div className="flex gap-2">
            <button className={backupActionClass} onClick={() => setShowRejectedList(false)} type="button">Close</button>
            <button className={backupActionClass} onClick={() => void loadRejectedMessages()} type="button" disabled={loadingRejected || reprocessingAll}>
              {loadingRejected ? "Loading..." : "Refresh"}
            </button>
            <button className="h-12 rounded-lg bg-brand-500 px-5 text-sm font-bold text-white hover:bg-brand-600 disabled:opacity-60" onClick={() => void reprocessAllRejectedMessages()} type="button" disabled={!rejectedMessages.some((row) => row.status.toLowerCase() === "pending") || reprocessingAll || loadingRejected}>
              {reprocessingAll ? "Sending..." : `Reprocess Pending (${rejectedMessages.filter((row) => row.status.toLowerCase() === "pending").length})`}
            </button>
          </div>
        </div>
        {message ? (
          <div className={message.kind === "ok"
            ? "border-b border-emerald-200 bg-emerald-50 px-5 py-3 text-sm font-medium text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-300"
            : "border-b border-red-200 bg-red-50 px-5 py-3 text-sm font-medium text-red-700 dark:border-red-500/20 dark:bg-red-500/10 dark:text-red-300"}>
            {message.text}
          </div>
        ) : null}
        {reprocessingAll ? (
          <div className="flex items-center gap-3 border-b border-blue-200 bg-blue-50 px-5 py-3 text-sm font-semibold text-blue-800 dark:border-blue-500/20 dark:bg-blue-500/10 dark:text-blue-200" role="status" aria-live="polite">
            <span className="size-4 animate-spin rounded-full border-2 border-blue-300 border-t-blue-700 dark:border-blue-500/40 dark:border-t-blue-200" aria-hidden="true" />
            <span>On Progress — sedang mengirim payload. Loading time: {Math.floor(reprocessElapsedSeconds / 60)}m {reprocessElapsedSeconds % 60}s</span>
          </div>
        ) : null}
        <div className="overflow-auto p-5">
          <table className="leak-rounded-header-table w-full min-w-[1120px] border-separate border-spacing-0 text-left text-sm">
            <thead className="bg-transparent text-xs uppercase text-white">
              <tr>
                <th className="rounded-l-lg bg-brand-500 px-4 py-3">ID</th>
                <th className="bg-brand-500 px-4 py-3">Received</th>
                <th className="bg-brand-500 px-4 py-3">Topic</th>
                <th className="bg-brand-500 px-4 py-3">Error</th>
                <th className="bg-brand-500 px-4 py-3">Status</th>
                <th className="bg-brand-500 px-4 py-3">Attempts</th>
                <th className="bg-brand-500 px-4 py-3">Pressure Input</th>
                <th className="bg-brand-500 px-4 py-3">Press Set Up</th>
                <th className="rounded-r-lg bg-brand-500 px-4 py-3">Press Set Low</th>
              </tr>
            </thead>
            <tbody>
              {loadingRejected && rejectedMessages.length === 0 ? <tr><td className="px-4 py-5 text-slate-500" colSpan={9}>Loading...</td></tr> : null}
              {!loadingRejected && rejectedMessages.length === 0 ? <tr><td className="px-4 py-5 text-slate-500" colSpan={9}>No rejected MQTT messages.</td></tr> : null}
              {rejectedMessages.map((row) => (
                <tr key={row.id}>
                  <td className="border-b border-slate-100 px-4 py-3 font-mono text-slate-700 dark:border-slate-800 dark:text-slate-300">{row.id}</td>
                  <td className="border-b border-slate-100 px-4 py-3 text-slate-700 dark:border-slate-800 dark:text-slate-300">{new Date(row.received_at).toLocaleString()}</td>
                  <td className="max-w-xs border-b border-slate-100 px-4 py-3 font-semibold text-slate-800 dark:border-slate-800 dark:text-slate-200">{row.topic}</td>
                  <td className="max-w-md border-b border-slate-100 px-4 py-3 text-rose-700 dark:border-slate-800 dark:text-rose-300">{row.error_message}</td>
                  <td className="border-b border-slate-100 px-4 py-3 dark:border-slate-800">{row.status}</td>
                  <td className="border-b border-slate-100 px-4 py-3 dark:border-slate-800">{row.attempt_count}</td>
                  <td className="border-b border-slate-100 px-2 py-2 dark:border-slate-800"><input aria-label={`Pressure Input ${row.id}`} className={tableInputClass} inputMode="decimal" value={row.pressureInput} onChange={(event) => setRejectedMessages((current) => current.map((item) => item.id === row.id ? { ...item, pressureInput: event.target.value } : item))} /></td>
                  <td className="border-b border-slate-100 px-2 py-2 dark:border-slate-800"><input aria-label={`Press Set Up ${row.id}`} className={tableInputClass} inputMode="decimal" value={row.pressSetUp} onChange={(event) => setRejectedMessages((current) => current.map((item) => item.id === row.id ? { ...item, pressSetUp: event.target.value } : item))} /></td>
                  <td className="border-b border-slate-100 px-2 py-2 dark:border-slate-800"><input aria-label={`Press Set Low ${row.id}`} className={tableInputClass} inputMode="decimal" value={row.pressSetLow} onChange={(event) => setRejectedMessages((current) => current.map((item) => item.id === row.id ? { ...item, pressSetLow: event.target.value } : item))} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      </div>
      ) : null}
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
