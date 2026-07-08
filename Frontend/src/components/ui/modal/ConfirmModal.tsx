import React from "react";
import { Modal } from "./index";
import { AlertIcon } from "@/icons";

interface ConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  isDestructive?: boolean;
  isLoading?: boolean;
}

export const ConfirmModal: React.FC<ConfirmModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = "Confirm",
  cancelText = "Cancel",
  isDestructive = false,
  isLoading = false,
}) => {
  const iconClasses = isDestructive
    ? "bg-error-50 text-error-600 ring-error-500/15 dark:bg-error-500/10 dark:text-error-400 dark:ring-error-500/25"
    : "bg-warning-50 text-warning-600 ring-warning-500/15 dark:bg-warning-500/10 dark:text-warning-400 dark:ring-warning-500/25";

  return (
    <Modal isOpen={isOpen} onClose={onClose} className="mx-4 max-w-[460px] overflow-hidden p-0" showCloseButton={false}>
      <div className="p-6 sm:p-7">
        <div className="flex items-start gap-4">
          <div className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-full ring-8 ${iconClasses}`}>
            <AlertIcon className="h-6 w-6 fill-current" />
          </div>
          <div className="min-w-0">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white/90">
              {title}
            </h3>
            <p className="mt-2 text-sm leading-6 text-gray-500 dark:text-gray-400">
              {message}
            </p>
          </div>
        </div>

        <div className="mt-7 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onClose}
            disabled={isLoading}
            className="inline-flex h-11 items-center justify-center rounded-lg border border-gray-300 bg-white px-5 text-sm font-semibold text-gray-700 shadow-theme-xs transition-colors hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-200 dark:hover:bg-white/[0.04]"
          >
            {cancelText}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={isLoading}
            className={`inline-flex h-11 min-w-28 items-center justify-center rounded-lg px-5 text-sm font-semibold text-white shadow-theme-xs transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${
              isDestructive
                ? "bg-error-500 hover:bg-error-600 focus:ring-3 focus:ring-error-500/20"
                : "bg-brand-500 hover:bg-brand-600 focus:ring-3 focus:ring-brand-500/20"
            }`}
          >
            {isLoading ? "Processing..." : confirmText}
          </button>
        </div>
      </div>
    </Modal>
  );
};
