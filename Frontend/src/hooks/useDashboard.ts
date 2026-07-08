"use client";

import { useCallback, useEffect, useState } from "react";
import DashboardService, {
  DashboardRecentLog,
  DashboardStats,
  DashboardSummary,
} from "@/services/DashboardService";

type DashboardData = {
  recentLogs: DashboardRecentLog[];
  stats: DashboardStats;
  summary: DashboardSummary;
};

const getErrorMessage = (error: unknown, fallback: string) =>
  error instanceof Error ? error.message : fallback;

export const useDashboard = (recentLogCount = 10) => {
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [reloadKey, setReloadKey] = useState(0);

  const refetch = useCallback(() => {
    setIsLoading(true);
    setError(null);
    setReloadKey((current) => current + 1);
  }, []);

  useEffect(() => {
    const controller = new AbortController();

    Promise.all([
      DashboardService.getSummary({ signal: controller.signal }),
      DashboardService.getStats({ signal: controller.signal }),
      DashboardService.getRecentLogs(recentLogCount, {
        signal: controller.signal,
      }),
    ])
      .then(([summaryResponse, statsResponse, recentLogsResponse]) => {
        setData({
          summary: summaryResponse.data,
          stats: statsResponse.data,
          recentLogs: recentLogsResponse.data,
        });
      })
      .catch((fetchError: unknown) => {
        if (
          fetchError instanceof DOMException &&
          fetchError.name === "AbortError"
        ) {
          return;
        }

        setError(getErrorMessage(fetchError, "Failed to load dashboard"));
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [recentLogCount, reloadKey]);

  return {
    data,
    error,
    isLoading,
    recentLogs: data?.recentLogs ?? [],
    refetch,
    stats: data?.stats,
    summary: data?.summary,
  };
};
