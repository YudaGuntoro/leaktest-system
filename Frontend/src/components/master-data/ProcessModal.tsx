"use client";

import React, { useEffect, useMemo, useState } from "react";
import { Modal } from "@/components/ui/modal";
import { useToast } from "@/context/ToastContext";
import ParameterService, { Parameter } from "@/services/ParameterService";
import ProcessService, {
  Process,
  ProcessPayload,
} from "@/services/ProcessService";

interface ProcessModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  process: Process | null;
}

type ProcessFormData = ProcessPayload;

const initialFormData: ProcessFormData = {
  code: "",
  name: "",
  description: "",
  isActive: true,
  parameterIds: [],
};

const getErrorMessage = (error: unknown, fallback: string) =>
  error instanceof Error ? error.message : fallback;

export default function ProcessModal({
  isOpen,
  onClose,
  onSuccess,
  process,
}: ProcessModalProps) {
  const toast = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoadingParameters, setIsLoadingParameters] = useState(false);
  const [parameters, setParameters] = useState<Parameter[]>([]);
  const [formData, setFormData] =
    useState<ProcessFormData>(initialFormData);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (process) {
      setFormData({
        code: process.code || "",
        name: process.name || "",
        description: process.description || "",
        isActive: process.isActive ?? true,
        parameterIds: process.parameters?.map((parameter) => parameter.id) ?? [],
      });
      return;
    }

    setFormData(initialFormData);
  }, [isOpen, process]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const controller = new AbortController();
    setIsLoadingParameters(true);

    ParameterService.getParameters(
      {
        isActive: true,
        limit: 100,
        page: 1,
      },
      {
        signal: controller.signal,
      }
    )
      .then((result) => {
        setParameters(result.data);
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        toast.error({
          title: "Failed to load parameters",
          message: getErrorMessage(error, "Failed to load parameters"),
        });
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setIsLoadingParameters(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [isOpen, toast]);

  const selectedParameterIdSet = useMemo(
    () => new Set(formData.parameterIds),
    [formData.parameterIds]
  );

  const handleChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, type, value } = event.target;
    const checked = (event.target as HTMLInputElement).checked;

    setFormData((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleParameterToggle = (parameterId: number) => {
    setFormData((current) => {
      const isSelected = current.parameterIds.includes(parameterId);

      return {
        ...current,
        parameterIds: isSelected
          ? current.parameterIds.filter((id) => id !== parameterId)
          : [...current.parameterIds, parameterId],
      };
    });
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      if (process) {
        await ProcessService.updateProcess(process.id, formData);
        toast.success({
          title: "Success",
          message: "Process updated successfully",
        });
      } else {
        await ProcessService.createProcess(formData);
        toast.success({
          title: "Success",
          message: "Process created successfully",
        });
      }

      onSuccess();
      onClose();
    } catch (error: unknown) {
      toast.error({
        title: "Error",
        message: getErrorMessage(
          error,
          `Failed to ${process ? "update" : "create"} process`
        ),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} className="max-w-[640px] p-6">
      <h3 className="mb-4 text-xl font-semibold text-gray-800 dark:text-white/90">
        {process ? "Update Process" : "Create Process"}
      </h3>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Code
          </label>
          <input
            type="text"
            name="code"
            value={formData.code}
            onChange={handleChange}
            required
            className="h-10 w-full rounded-lg border border-gray-300 bg-transparent px-3 py-2 text-sm text-gray-800 outline-none placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 dark:border-gray-700 dark:bg-gray-900 dark:text-white/90"
            placeholder="Enter process code"
          />
        </div>

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
            placeholder="Enter process name"
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
            placeholder="Enter process description"
          />
        </div>

        <div>
          <div className="mb-1.5 flex items-center justify-between">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              Parameters
            </label>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {formData.parameterIds.length} selected
            </span>
          </div>
          <div className="max-h-52 overflow-y-auto rounded-lg border border-gray-300 p-3 dark:border-gray-700 dark:bg-gray-900">
            {isLoadingParameters && (
              <p className="text-sm text-gray-500 dark:text-gray-400">
                Loading parameters...
              </p>
            )}

            {!isLoadingParameters && parameters.length === 0 && (
              <p className="text-sm text-gray-500 dark:text-gray-400">
                No active parameters found
              </p>
            )}

            {!isLoadingParameters &&
              parameters.map((parameter) => (
                <label
                  key={parameter.id}
                  className="flex cursor-pointer items-start gap-3 rounded-md px-2 py-2 hover:bg-gray-50 dark:hover:bg-white/[0.04]"
                >
                  <input
                    type="checkbox"
                    checked={selectedParameterIdSet.has(parameter.id)}
                    onChange={() => handleParameterToggle(parameter.id)}
                    className="mt-1 h-4 w-4 rounded border-gray-300 text-brand-500 focus:ring-brand-500"
                  />
                  <span>
                    <span className="block text-sm font-medium text-gray-800 dark:text-white/90">
                      {parameter.code} - {parameter.name}
                    </span>
                    <span className="block text-xs text-gray-500 dark:text-gray-400">
                      {parameter.dataType}
                    </span>
                  </span>
                </label>
              ))}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="process-is-active"
            name="isActive"
            checked={formData.isActive}
            onChange={handleChange}
            className="h-4 w-4 rounded border-gray-300 text-brand-500 focus:ring-brand-500"
          />
          <label
            htmlFor="process-is-active"
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
