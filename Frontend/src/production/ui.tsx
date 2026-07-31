import DatePicker from "@/components/form/date-picker";

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
