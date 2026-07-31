"use client";

import { useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api";
import {
  API_ACTIVITY_EVENT,
  type ApiActivityEventDetail,
} from "@/lib/api-activity";

type LeaktesterStatus = {
  last_mqtt_at?: string | null;
  server_time?: string;
};

const MQTT_STALE_MS = 60_000;
const STATUS_POLL_MS = 10_000;

const timeFormatter = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});

function parseDate(value?: string | null) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function formatTime(value?: string | null) {
  const date = parseDate(value);
  return date ? timeFormatter.format(date) : "--:--:--";
}

export default function MqttStatus() {
  const [lastApiAt, setLastApiAt] = useState<string | null>(null);
  const [lastMqttAt, setLastMqttAt] = useState<string | null>(null);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const handleApiActivity = (event: Event) => {
      const detail = (event as CustomEvent<ApiActivityEventDetail>).detail;
      setLastApiAt(detail?.at ?? null);
    };

    window.addEventListener(API_ACTIVITY_EVENT, handleApiActivity);
    return () => window.removeEventListener(API_ACTIVITY_EVENT, handleApiActivity);
  }, []);

  useEffect(() => {
    let ignore = false;

    const loadStatus = async () => {
      try {
        const status = await apiGet<LeaktesterStatus>("/api/leaktester/status");
        if (!ignore) {
          setLastMqttAt(status.last_mqtt_at ?? null);
        }
      } catch {
        if (!ignore) {
          setLastMqttAt((current) => current);
        }
      }
    };

    void loadStatus();
    const timer = window.setInterval(() => void loadStatus(), STATUS_POLL_MS);

    return () => {
      ignore = true;
      window.clearInterval(timer);
    };
  }, []);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const mqttOnline = useMemo(() => {
    const date = parseDate(lastMqttAt);
    return Boolean(date && now - date.getTime() <= MQTT_STALE_MS);
  }, [lastMqttAt, now]);

  return (
    <div className="flex h-11 max-w-full items-center gap-3 overflow-hidden rounded-lg border border-gray-200 bg-white px-3 text-xs font-semibold text-gray-500 shadow-theme-xs dark:border-gray-800 dark:bg-gray-900 dark:text-gray-400 sm:px-4">
      <span className={`inline-flex shrink-0 items-center gap-1.5 rounded-full px-2.5 py-1 font-bold ${mqttOnline ? "bg-emerald-50 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-300" : "bg-rose-50 text-rose-600 dark:bg-rose-500/10 dark:text-rose-300"}`}>
        <span className={`size-2 rounded-full ${mqttOnline ? "bg-emerald-500" : "bg-rose-500"}`} />
        {mqttOnline ? "Online" : "Offline"}
      </span>
      <span className="h-5 w-px shrink-0 bg-gray-200 dark:bg-gray-800" />
      <span className="truncate">
        Last Update: <span className="font-bold text-gray-700 dark:text-gray-200">API {formatTime(lastApiAt)}</span>
      </span>
      <span className="h-5 w-px shrink-0 bg-gray-200 dark:bg-gray-800" />
      <span className="shrink-0 font-bold text-gray-700 dark:text-gray-200">MQTT {formatTime(lastMqttAt)}</span>
    </div>
  );
}
