import type { ButtonHTMLAttributes, ReactNode } from "react";
import { twMerge } from "tailwind-merge";

type ExportButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children?: ReactNode;
};

export default function ExportButton({
  children = "Export",
  className,
  type = "button",
  ...props
}: ExportButtonProps) {
  return (
    <button
      className={twMerge(
        "inline-flex h-10 items-center justify-center gap-2.5 rounded-lg border border-[#168348] bg-[#21A366] px-4 text-sm font-semibold text-white shadow-theme-xs transition-colors hover:bg-[#1E8E59] focus:outline-none focus:ring-3 focus:ring-[#21A366]/25 active:bg-[#107C41] disabled:cursor-not-allowed disabled:opacity-60",
        className,
      )}
      type={type}
      {...props}
    >
      <span className="leading-5">{children}</span>
      <span className="inline-flex size-5 shrink-0 items-center justify-center" aria-hidden="true">
        <svg className="h-5 w-5 overflow-visible" viewBox="0 0 24 24" fill="none">
          <path d="M8 4.5h8.25L20 8.25V19a1.5 1.5 0 0 1-1.5 1.5H8V4.5Z" fill="#ffffff" opacity="0.95" />
          <path d="M16.25 4.5v3.75H20" fill="#DFF6EA" />
          <path d="M8 4.5h8.25L20 8.25V19a1.5 1.5 0 0 1-1.5 1.5H8V4.5Z" stroke="#ffffff" strokeWidth="1.2" strokeLinejoin="round" />
          <path d="M16.25 4.5v3.75H20" stroke="#16A34A" strokeWidth="1.2" strokeLinejoin="round" />
          <path d="M4 7.25 12.5 5.8v12.4L4 16.75V7.25Z" fill="#107C41" />
          <path d="m6.35 10.1 1.2 2 1.24-2h1.45l-1.86 2.92 1.95 3.08H8.84l-1.31-2.15-1.33 2.15H4.75l1.98-3.08L4.9 10.1h1.45Z" fill="#ffffff" />
          <path d="M13.55 10.3h3.55M13.55 12.5h3.55M13.55 14.7h3.55" stroke="#16A34A" strokeWidth="1.05" strokeLinecap="round" />
        </svg>
      </span>
    </button>
  );
}
