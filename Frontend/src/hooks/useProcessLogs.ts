"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ApiListResponse } from "@/services/ParameterService";
import ProcessLogService, {
  ProcessLog,
  ProcessLogQuery,
} from "@/services/ProcessLogService";

type UseProcessLogsOptions = ProcessLogQuery & {
  enabled?: boolean;
};

export type ProcessLogQueryState = {
  page: number;
  limit: number;
  issueNo: string;
  partNumber: string;
  isActive?: boolean | null;
};

const getInitialQuery = (
  options: UseProcessLogsOptions
): ProcessLogQueryState => ({
  page: options.page ?? 1,
  limit: options.limit ?? 10,
  issueNo: options.issueNo ?? "",
  partNumber: options.partNumber ?? "",
  isActive: options.isActive,
});

export const useProcessLogs = (options: UseProcessLogsOptions = {}) => {
  const { enabled = true } = options;
  const [query, setQueryState] = useState<ProcessLogQueryState>(() =>
    getInitialQuery(options)
  );
  const [response, setResponse] =
    useState<ApiListResponse<ProcessLog> | null>(null);
  const [isLoading, setIsLoading] = useState(enabled);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const requestQuery = useMemo(
    () => ({
      page: query.page,
      limit: query.limit,
      issueNo: query.issueNo,
      partNumber: query.partNumber,
      isActive: query.isActive,
    }),
    [
      query.isActive,
      query.issueNo,
      query.limit,
      query.page,
      query.partNumber,
    ]
  );

  const startRequest = useCallback(() => {
    if (enabled) {
      setIsLoading(true);
      setError(null);
    }
  }, [enabled]);

  const setPage = useCallback(
    (page: number) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        page,
      }));
    },
    [startRequest]
  );

  const setLimit = useCallback(
    (limit: number) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        limit,
        page: 1,
      }));
    },
    [startRequest]
  );

  const setQuery = useCallback(
    (nextQuery: Partial<ProcessLogQueryState>) => {
      startRequest();
      setQueryState((current) => ({
        ...current,
        ...nextQuery,
        page: 1,
      }));
    },
    [startRequest]
  );

  const refetch = useCallback(() => {
    startRequest();
    setReloadKey((current) => current + 1);
  }, [startRequest]);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const controller = new AbortController();

    ProcessLogService.getProcessLogs(requestQuery, {
      signal: controller.signal,
    })
      .then((result) => {
        setResponse(result);
      })
      .catch((fetchError: unknown) => {
        if (
          fetchError instanceof DOMException &&
          fetchError.name === "AbortError"
        ) {
          return;
        }

        setError(
          fetchError instanceof Error
            ? fetchError.message
            : "Failed to fetch process logs"
        );
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [enabled, reloadKey, requestQuery]);

  return {
    data: response?.data ?? [],
    error,
    isLoading,
    pagination: response?.pagination,
    query,
    refetch,
    response,
    setLimit,
    setPage,
    setQuery,
  };
};
