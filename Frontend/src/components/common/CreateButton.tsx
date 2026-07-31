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
        "primary-button",
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
