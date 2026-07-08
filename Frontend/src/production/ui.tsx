import type { ProductionWorkOrderStatus } from "./types";
import DatePicker from "@/components/form/date-picker";

const styles: Record<ProductionWorkOrderStatus, string> = {
  WAITING: "bg-amber-50 text-amber-700 ring-amber-200 dark:bg-amber-500/10 dark:text-amber-300 dark:ring-amber-500/20",
  READY: "bg-sky-50 text-sky-700 ring-sky-200 dark:bg-sky-500/10 dark:text-sky-300 dark:ring-sky-500/20",
  IN_PROGRESS: "bg-blue-50 text-blue-700 ring-blue-200 dark:bg-blue-500/10 dark:text-blue-300 dark:ring-blue-500/20",
  HOLD: "bg-orange-50 text-orange-700 ring-orange-200 dark:bg-orange-500/10 dark:text-orange-300 dark:ring-orange-500/20",
  COMPLETED: "bg-emerald-50 text-emerald-700 ring-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-300 dark:ring-emerald-500/20",
  CANCELLED: "bg-slate-100 text-slate-600 ring-slate-200 dark:bg-slate-700/30 dark:text-slate-300 dark:ring-slate-600",
};

export function StatusBadge({ status }: { status: ProductionWorkOrderStatus }) {
  return <span className={`inline-flex rounded-full px-2.5 py-1 text-[11px] font-bold ring-1 ring-inset ${styles[status]}`}>{status.replaceAll("_", " ")}</span>;
}

export function formatDateTime(value?: string | null) {
  if (!value) return "-";
  return new Intl.DateTimeFormat("en-GB", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function todayParam() {
  const date = new Date();
  const offset = date.getTimezoneOffset();
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 10);
}

type ProductionDatePickerProps = {
  className?: string;
  defaultValue?: string;
  label?: string;
  name?: string;
  onChange?: (value: string) => void;
  required?: boolean;
  value?: string;
};

export function ProductionDatePicker({
  className = "",
  defaultValue,
  label,
  name,
  onChange,
  required,
  value,
}: ProductionDatePickerProps) {
  const id = `production-date-${(name || label || "picker").toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;

  return (
    <div className={className}>
      <DatePicker
        altFormat="d / m / Y"
        altInput
        className="h-10 rounded-md border-slate-300 bg-white px-4 pr-10 text-sm font-black text-slate-900 dark:border-slate-600 dark:bg-slate-950 dark:text-white"
        dateFormat="Y-m-d"
        defaultDate={value ?? defaultValue}
        id={id}
        label={label}
        name={name}
        onChange={(_, dateStr) => onChange?.(dateStr)}
        required={required}
      />
    </div>
  );
}
