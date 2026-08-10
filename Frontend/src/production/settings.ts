import { apiGet, apiRequest } from "@/lib/api";

export type BackupSchedule = "daily" | "weekly" | "monthly";

export type UnitSettings = {
  pressureUnit: string;
  cycleTimeUnit: string;
};

export type SystemSettings = UnitSettings & {
  backupDbLocation: string;
  plcIpAddress: string;
  schedule: BackupSchedule;
};

export const SYSTEM_SETTINGS_STORAGE_KEY = "yanmar-leaktester-backup-settings";

export const defaultSystemSettings: SystemSettings = {
  backupDbLocation: "",
  cycleTimeUnit: "s",
  plcIpAddress: "",
  pressureUnit: "MPa",
  schedule: "daily",
};

type ApiSystemSettings = {
  pressure_unit: string;
  cycle_time_unit: string;
  backup_db_location: string;
  backup_schedule: BackupSchedule;
  plc_ip_address?: string | null;
};

function fromApiSettings(settings: ApiSystemSettings): SystemSettings {
  return {
    backupDbLocation: settings.backup_db_location ?? "",
    cycleTimeUnit: settings.cycle_time_unit ?? defaultSystemSettings.cycleTimeUnit,
    plcIpAddress: settings.plc_ip_address ?? "",
    pressureUnit: settings.pressure_unit ?? defaultSystemSettings.pressureUnit,
    schedule: settings.backup_schedule ?? defaultSystemSettings.schedule,
  };
}

function toApiSettings(settings: SystemSettings) {
  return {
    backup_db_location: settings.backupDbLocation,
    backup_schedule: settings.schedule,
    cycle_time_unit: settings.cycleTimeUnit,
    plc_ip_address: settings.plcIpAddress,
    pressure_unit: settings.pressureUnit,
  };
}

export function readSystemSettings(): SystemSettings {
  if (typeof window === "undefined") {
    return defaultSystemSettings;
  }

  try {
    const stored = window.localStorage.getItem(SYSTEM_SETTINGS_STORAGE_KEY);
    return stored ? { ...defaultSystemSettings, ...JSON.parse(stored) } : defaultSystemSettings;
  } catch {
    return defaultSystemSettings;
  }
}

export function saveSystemSettings(settings: SystemSettings) {
  window.localStorage.setItem(SYSTEM_SETTINGS_STORAGE_KEY, JSON.stringify(settings));
}

export async function fetchSystemSettings() {
  try {
    const settings = fromApiSettings(await apiGet<ApiSystemSettings>("/api/leaktester/settings"));
    saveSystemSettings(settings);
    return settings;
  } catch {
    return readSystemSettings();
  }
}

export async function updateSystemSettings(settings: SystemSettings) {
  const updated = fromApiSettings(await apiRequest<ApiSystemSettings>("/api/leaktester/settings", {
    body: JSON.stringify(toApiSettings(settings)),
    method: "PUT",
  }));
  saveSystemSettings(updated);
  return updated;
}

export function getUnitSettings(): UnitSettings {
  const settings = readSystemSettings();
  return {
    cycleTimeUnit: settings.cycleTimeUnit,
    pressureUnit: settings.pressureUnit,
  };
}

export function displayNumber(value: number, fractionDigits = 2) {
  return Number(value).toFixed(fractionDigits);
}

export function displayUnitlessText(value?: string | null) {
  if (!value || !value.trim()) {
    return "-";
  }

  return value
    .replace(/\s*\b(MPa|kPa|Pa|bar|psi)\b/gi, "")
    .replace(/\s+/g, " ")
    .trim();
}
