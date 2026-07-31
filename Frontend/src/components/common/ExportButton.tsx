import type { ButtonHTMLAttributes, ReactNode } from "react";
import { twMerge } from "tailwind-merge";
import { DownloadIcon } from "@/icons";

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
      <span className="inline-flex size-5 shrink-0 items-center justify-center">
        <DownloadIcon className="h-[18px] w-[18px] overflow-visible" />
      </span>
    </button>
  );
}
