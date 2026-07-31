"use client";

import type { ButtonHTMLAttributes } from "react";
import { twMerge } from "tailwind-merge";
import { CloseIcon } from "@/icons";

type ClearFilterButtonProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, "children"> & {
  label?: string;
};

export default function ClearFilterButton({
  className,
  label = "Clear filter",
  title,
  type = "button",
  ...props
}: ClearFilterButtonProps) {
  return (
    <button
      aria-label={label}
      className={twMerge(
        "inline-flex size-10 shrink-0 items-center justify-center rounded-md text-slate-400 transition hover:bg-slate-100 hover:text-brand-500 focus:outline-none focus:ring-3 focus:ring-brand-500/20 disabled:cursor-not-allowed disabled:bg-transparent disabled:opacity-40 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white",
        className
      )}
      title={title ?? label}
      type={type}
      {...props}
    >
      <CloseIcon className="size-5" />
    </button>
  );
}
