"use client";

import React, { useState, useEffect, useMemo, useRef } from "react";
import { Modal } from "@/components/ui/modal";
import StockInService, { StockIn } from "@/services/StockInService";
import { useToast } from "@/context/ToastContext";
import { useParts } from "@/hooks/useParts";
import { Part } from "@/services/PartService";
import DatePicker from "../form/date-picker";

const getErrorMessage = (error: unknown, fallback: string) =>
  error instanceof Error ? error.message : fallback;

const dateInputClassName =
  "h-10 px-3 py-2 pr-10 focus:border-brand-300 focus:ring-brand-500/10 dark:focus:border-brand-800";

const partSelectButtonClassName =
  "flex h-10 w-full items-center justify-between rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-left text-sm text-gray-800 outline-none transition focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 disabled:cursor-not-allowed disabled:opacity-60 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90";

interface StockInModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  stockIn: StockIn | null;
}

type PartSelectProps = {
  disabled?: boolean;
  fallbackPart?: Pick<Part, "id" | "name" | "number"> | null;
  isLoading?: boolean;
  onChange: (partId: number) => void;
  parts: Part[];
  value: number;
};

function PartSearchSelect({
  disabled = false,
  fallbackPart,
  isLoading = false,
  onChange,
  parts,
  value,
}: PartSelectProps) {
  const wrapperRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState("");

  const selectedPart =
    parts.find((part) => part.id === value) ??
    (fallbackPart?.id === value ? fallbackPart : null);
  const selectedLabel = selectedPart
    ? `${selectedPart.number} - ${selectedPart.name}`
    : "";
  const normalizedSearch = search.trim().toLowerCase();
  const filteredParts = useMemo(() => {
    const matchedParts = normalizedSearch
      ? parts.filter((part) =>
          `${part.number} ${part.name}`.toLowerCase().includes(normalizedSearch)
        )
      : parts;

    return matchedParts.slice(0, 60);
  }, [normalizedSearch, parts]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        wrapperRef.current &&
        !wrapperRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
        setSearch("");
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const openDropdown = () => {
    if (disabled || isLoading) return;
    setIsOpen(true);
    globalThis.setTimeout(() => inputRef.current?.focus(), 0);
  };

  const handleSelect = (partId: number) => {
    onChange(partId);
    setIsOpen(false);
    setSearch("");
  };

  return (
    <div ref={wrapperRef} className="relative">
      <button
        className={partSelectButtonClassName}
        disabled={disabled || isLoading}
        onClick={openDropdown}
        type="button"
      >
        <span className={selectedLabel ? "" : "text-gray-400"}>
          {isLoading
            ? "Loading parts..."
            : selectedLabel || "Search and select a part"}
        </span>
        <svg
          className={`ml-2 size-5 shrink-0 stroke-current text-gray-500 transition-transform ${
            isOpen ? "rotate-180" : ""
          }`}
          viewBox="0 0 20 20"
          fill="none"
        >
          <path
            d="M4.792 7.396 10 12.604l5.208-5.208"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </button>

      {isOpen && (
        <div className="absolute left-0 top-full z-999 mt-1 w-full overflow-hidden rounded-lg border border-gray-200 bg-white shadow-theme-lg dark:border-gray-700 dark:bg-gray-900">
          <div className="border-b border-gray-100 p-2 dark:border-gray-800">
            <input
              ref={inputRef}
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
              placeholder="Type part number or name"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>

          <div className="max-h-64 overflow-y-auto py-1">
            {filteredParts.length > 0 ? (
              filteredParts.map((part) => (
                <button
                  key={part.id}
                  className={`flex w-full flex-col px-3 py-2 text-left text-sm transition-colors hover:bg-gray-50 dark:hover:bg-white/[0.04] ${
                    part.id === value
                      ? "bg-brand-50 text-brand-600 dark:bg-brand-500/10 dark:text-brand-300"
                      : "text-gray-700 dark:text-gray-300"
                  }`}
                  onClick={() => handleSelect(part.id)}
                  type="button"
                >
                  <span className="font-medium">{part.number}</span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">
                    {part.name}
                  </span>
                </button>
              ))
            ) : (
              <p className="px-3 py-6 text-center text-sm text-gray-500 dark:text-gray-400">
                No parts found
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default function StockInModal({
  isOpen,
  onClose,
  onSuccess,
  stockIn,
}: StockInModalProps) {
  const toast = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { data: parts, isLoading: isLoadingParts } = useParts({ limit: 1000 });

  const [formData, setFormData] = useState({
    partId: 0,
    supplyQty: 0,
    supplyDate: new Date().toISOString(),
    receiptQty: 0,
    receiptDate: new Date().toISOString(),
  });

  useEffect(() => {
    if (isOpen) {
      if (stockIn) {
        setFormData({
          partId: stockIn.partId,
          supplyQty: stockIn.supplyQty,
          supplyDate: stockIn.supplyDate,
          receiptQty: stockIn.receiptQty,
          receiptDate: stockIn.receiptDate,
        });
      } else {
        setFormData({
          partId: 0,
          supplyQty: 0,
          supplyDate: new Date().toISOString(),
          receiptQty: 0,
          receiptDate: new Date().toISOString(),
        });
      }
    }
  }, [isOpen, stockIn]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value, type } = e.target;
    
    setFormData((prev) => ({
      ...prev,
      [name]: type === "number" ? Number(value) : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (formData.partId === 0) {
      toast.error({ title: "Error", message: "Please select a part" });
      return;
    }

    setIsSubmitting(true);
    try {
      if (stockIn) {
        await StockInService.updateStockIn(stockIn.id, formData);
        toast.success({
          title: "Success",
          message: "Stock in record updated successfully",
        });
      } else {
        await StockInService.createStockIn(formData);
        toast.success({
          title: "Success",
          message: "Stock in record created successfully",
        });
      }
      onSuccess();
      onClose();
    } catch (err: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(
          err,
          `Failed to ${stockIn ? "update" : "create"} stock in record`
        ),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} className="max-w-[500px] p-6">
      <h3 className="mb-4 text-xl font-semibold text-gray-800 dark:text-white/90">
        {stockIn ? "Update Stock In" : "Create Stock In"}
      </h3>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Part
          </label>
          <PartSearchSelect
            fallbackPart={stockIn?.part ?? null}
            isLoading={isLoadingParts}
            onChange={(partId) =>
              setFormData((prev) => ({
                ...prev,
                partId,
              }))
            }
            parts={parts}
            value={formData.partId}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Supply Quantity
            </label>
            <input
              type="text"
              name="supplyQty"
              value={formData.supplyQty}
              onChange={handleChange}
              required
              min={0}
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            />
          </div>
          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Receipt Quantity
            </label>
            <input
              type="text"
              name="receiptQty"
              value={formData.receiptQty}
              onChange={handleChange}
              required
              min={0}
              className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            />
          </div>
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Supply Date
          </label>
          <DatePicker
            id="supplyDate"
            enableTime
            className={dateInputClassName}
            defaultDate={formData.supplyDate}
            onChange={([date]) => {
              if (date) setFormData((prev) => ({ ...prev, supplyDate: date.toISOString() }));
            }}
            placeholder="Select supply date"
          />
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Receipt Date
          </label>
          <DatePicker
            id="receiptDate"
            enableTime
            className={dateInputClassName}
            defaultDate={formData.receiptDate}
            onChange={([date]) => {
              if (date) setFormData((prev) => ({ ...prev, receiptDate: date.toISOString() }));
            }}
            placeholder="Select receipt date"
          />
        </div>

        <div className="mt-4 flex justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-gray-700 dark:text-gray-300 dark:hover:bg-gray-800 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white hover:bg-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-500/50 disabled:opacity-50 transition-colors"
          >
            {isSubmitting ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </Modal>
  );
}
