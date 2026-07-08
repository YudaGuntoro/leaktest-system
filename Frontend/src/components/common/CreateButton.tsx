import type { ButtonHTMLAttributes, ReactNode } from "react";
import { twMerge } from "tailwind-merge";
import { PlusIcon } from "@/icons";

type CreateButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children?: ReactNode;
};

export default function CreateButton({
  children = "Create",
  className,
  type = "button",
  ...props
}: CreateButtonProps) {
  return (
    <button
      className={twMerge(
        "inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-[#6D8AF3] px-4 text-sm font-semibold text-white shadow-theme-xs transition-colors hover:bg-[#5f7eea] focus:outline-none focus:ring-3 focus:ring-[#6D8AF3]/25 disabled:cursor-not-allowed disabled:opacity-60",
        className
      )}
      type={type}
      {...props}
    >
      <span className="inline-flex size-4 shrink-0 items-center justify-center">
        <PlusIcon className="size-4" />
      </span>
      <span className="leading-5">{children}</span>
    </button>
  );
}
