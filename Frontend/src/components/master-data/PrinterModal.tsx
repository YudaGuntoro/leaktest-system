"use client";

import React, { useEffect, useState } from "react";
import { Modal } from "@/components/ui/modal";
import PrinterService, {
  Printer,
  PrinterPayload,
} from "@/services/PrinterService";
import { useToast } from "@/context/ToastContext";

interface PrinterModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  printer: Printer | null;
}

type PrinterFormData = Omit<PrinterPayload, "port"> & {
  port: string;
};

const initialFormData: PrinterFormData = {
  name: "",
  ipAddress: "",
  port: "9100",
  description: "",
  isActive: true,
};

const getErrorMessage = (error: unknown, fallback: string) =>
  error instanceof Error ? error.message : fallback;

export default function PrinterModal({
  isOpen,
  onClose,
  onSuccess,
  printer,
}: PrinterModalProps) {
  const toast = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] =
    useState<PrinterFormData>(initialFormData);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (printer) {
      setFormData({
        name: printer.name || "",
        ipAddress: printer.ipAddress || "",
        port: String(printer.port || ""),
        description: printer.description || "",
        isActive: printer.isActive ?? true,
      });
      return;
    }

    setFormData(initialFormData);
  }, [isOpen, printer]);

  const handleChange = (
    event: React.ChangeEvent<
      HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
    >
  ) => {
    const { name, type, value } = event.target;
    const checked = (event.target as HTMLInputElement).checked;

    setFormData((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);

    const payload: PrinterPayload = {
      ...formData,
      port: Number(formData.port),
    };

    try {
      if (printer) {
        await PrinterService.updatePrinter(printer.id, payload);
        toast.success({
          title: "Success",
          message: "Printer updated successfully",
        });
      } else {
        await PrinterService.createPrinter(payload);
        toast.success({
          title: "Success",
          message: "Printer created successfully",
        });
      }

      onSuccess();
      onClose();
    } catch (error: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(
          error,
          `Failed to ${printer ? "update" : "create"} printer`
        ),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} className="max-w-[500px] p-6">
      <h3 className="mb-4 text-xl font-semibold text-gray-800 dark:text-white/90">
        {printer ? "Update Printer" : "Create Printer"}
      </h3>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Name
          </label>
          <input
            type="text"
            name="name"
            value={formData.name}
            onChange={handleChange}
            required
            className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            placeholder="Enter printer name"
          />
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            IP Address
          </label>
          <input
            type="text"
            name="ipAddress"
            value={formData.ipAddress}
            onChange={handleChange}
            required
            className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            placeholder="Enter IP address"
          />
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Port
          </label>
          <input
            type="text"
            name="port"
            value={formData.port}
            onChange={handleChange}
            required
            min={1}
            max={65535}
            className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            placeholder="Enter port"
          />
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Description
          </label>
          <textarea
            name="description"
            value={formData.description}
            onChange={handleChange}
            rows={3}
            className="w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            placeholder="Enter printer description"
          />
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="printer-is-active"
            name="isActive"
            checked={formData.isActive}
            onChange={handleChange}
            className="h-4 w-4 rounded border-gray-300 text-brand-500 focus:ring-brand-500"
          />
          <label
            htmlFor="printer-is-active"
            className="text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            Active
          </label>
        </div>

        <div className="mt-4 flex justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-500/50 disabled:opacity-50"
          >
            {isSubmitting ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </Modal>
  );
}
