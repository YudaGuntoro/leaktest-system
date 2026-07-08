"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import StockInService, {
  StockIn,
  StockInQuery,
} from "@/services/StockInService";
import { ApiListResponse } from "@/services/ParameterService";

type UseStockInsOptions = StockInQuery & {
  enabled?: boolean;
};

type StockInQueryState = {
  page: number;
  limit: number;
  date?: string;
  issueNumber?: string;
  partNumber?: string;
};

const getInitialQuery = (options: UseStockInsOptions): StockInQueryState => ({
  page: options.page ?? 1,
  limit: options.limit ?? 10,
  date: options.date,
  issueNumber: options.issueNumber,
  partNumber: options.partNumber,
});

export const useStockIns = (options: UseStockInsOptions = {}) => {
  const { enabled = true } = options;
  const [query, setQueryState] = useState<StockInQueryState>(() =>
    getInitialQuery(options)
  );
  const [response, setResponse] = useState<ApiListResponse<StockIn> | null>(
    null
  );
  const [isLoading, setIsLoading] = useState(enabled);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const requestQuery = useMemo(
    () => ({
      page: query.page,
      limit: query.limit,
      date: query.date,
      issueNumber: query.issueNumber,
      partNumber: query.partNumber,
    }),
    [query.date, query.issueNumber, query.limit, query.page, query.partNumber]
  );

  const startRequest = useCallback(() => {
    if (enabled) {
      setIsLoading(true);
      setError(null);
    }
  }, [enabled]);

  const setPage = useCallback((page: number) => {
    startRequest();
    setQueryState((current) => ({
      ...current,
      page,
    }));
  }, [startRequest]);

  const setLimit = useCallback((limit: number) => {
    startRequest();
    setQueryState((current) => ({
      ...current,
      limit,
      page: 1,
    }));
  }, [startRequest]);

  const setQuery = useCallback((nextQuery: Partial<StockInQueryState>) => {
    startRequest();
    setQueryState((current) => ({
      ...current,
      ...nextQuery,
      page: 1, // Reset to page 1 on filter change
    }));
  }, [startRequest]);

  const refetch = useCallback(() => {
    startRequest();
    setReloadKey((current) => current + 1);
  }, [startRequest]);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const controller = new AbortController();

    StockInService.getStockIns(requestQuery, {
      signal: controller.signal,
    })
      .then((result) => {
        setResponse(result);
      })
      .catch((fetchError: unknown) => {
        if (fetchError instanceof DOMException && fetchError.name === "AbortError") {
          return;
        }

        setError(
          fetchError instanceof Error
            ? fetchError.message
            : "Failed to fetch stock ins"
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
