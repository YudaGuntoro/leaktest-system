import { useEffect, useRef } from 'react';
import flatpickr from 'flatpickr';
import 'flatpickr/dist/flatpickr.css';
import { twMerge } from 'tailwind-merge';
import Label from './Label';
import { CalenderIcon } from '../../icons';
import Hook = flatpickr.Options.Hook;
import DateOption = flatpickr.Options.DateOption;

type PropsType = {
  id: string;
  mode?: "single" | "multiple" | "range" | "time";
  onChange?: Hook | Hook[];
  defaultDate?: DateOption | DateOption[];
  label?: string;
  placeholder?: string;
  enableTime?: boolean;
  dateFormat?: string;
  className?: string;
  iconClassName?: string;
  name?: string;
  required?: boolean;
  altInput?: boolean;
  altFormat?: string;
  staticCalendar?: boolean;
};

export default function DatePicker({
  id,
  mode,
  onChange,
  label,
  defaultDate,
  placeholder,
  enableTime = false,
  dateFormat,
  className,
  iconClassName,
  name,
  required,
  altInput,
  altFormat,
  staticCalendar = false,
}: PropsType) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const resolvedDateFormat = dateFormat || (enableTime ? "Y-m-d H:i" : "Y-m-d");

  useEffect(() => {
    if (!inputRef.current) {
      return;
    }

    const flatPickr = flatpickr(inputRef.current, {
      mode: mode || "single",
      static: staticCalendar,
      monthSelectorType: "static",
      dateFormat: resolvedDateFormat,
      defaultDate,
      onChange,
      enableTime,
      time_24hr: true,
      allowInput: true,
      altInput,
      altFormat,
      prevArrow:
        '<svg class="stroke-current" width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M12.5 15L7.5 10L12.5 5" stroke="" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>',
      nextArrow:
        '<svg class="stroke-current" width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M7.5 15L12.5 10L7.5 5" stroke="" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    });

    return () => {
      flatPickr.destroy();
    };
  }, [mode, onChange, id, defaultDate, resolvedDateFormat, enableTime, altInput, altFormat, staticCalendar]);

  return (
    <div>
      {label && <Label htmlFor={id}>{label}</Label>}

      <div className="relative">
        <input
          id={id}
          name={name}
          ref={inputRef}
          placeholder={placeholder}
          required={required}
          className={twMerge(
            "h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs placeholder:text-gray-400 focus:outline-hidden focus:ring-3 bg-transparent text-gray-800 border-gray-300 focus:border-brand-400 focus:ring-brand-400/20 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90 dark:placeholder:text-white/30 dark:focus:border-brand-500",
            className
          )}
        />

        <span className="pointer-events-none absolute right-3.5 top-1/2 flex size-5 -translate-y-1/2 items-center justify-center text-brand-500 leading-none dark:text-brand-400">
          <CalenderIcon
            className={twMerge("block size-[18px] overflow-visible", iconClassName)}
          />
        </span>
      </div>
    </div>
  );
}
